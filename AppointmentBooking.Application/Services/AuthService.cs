using System.Security.Cryptography;
using AppointmentBooking.Application.Constants;
using AppointmentBooking.Application.DTOs.Auth;
using AppValidationException = AppointmentBooking.Application.Exceptions.ValidationException;
using AppointmentBooking.Application.Exceptions;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AppointmentBooking.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordResetTokenRepository _resetTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IValidator<RegisterRequestDto> _registerValidator;
    private readonly IValidator<UpdateProfileRequestDto> _updateProfileValidator;
    private readonly IValidator<ChangePasswordRequestDto> _changePasswordValidator;
    private readonly IValidator<ForgotPasswordRequestDto> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequestDto> _resetPasswordValidator;
    private readonly IValidator<AdminCreateUserRequestDto> _adminCreateUserValidator;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordResetTokenRepository resetTokenRepository,
        IJwtTokenService jwtTokenService,
        IValidator<RegisterRequestDto> registerValidator,
        IValidator<UpdateProfileRequestDto> updateProfileValidator,
        IValidator<ChangePasswordRequestDto> changePasswordValidator,
        IValidator<ForgotPasswordRequestDto> forgotPasswordValidator,
        IValidator<ResetPasswordRequestDto> resetPasswordValidator,
        IValidator<AdminCreateUserRequestDto> adminCreateUserValidator,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _resetTokenRepository = resetTokenRepository;
        _jwtTokenService = jwtTokenService;
        _registerValidator = registerValidator;
        _updateProfileValidator = updateProfileValidator;
        _changePasswordValidator = changePasswordValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _adminCreateUserValidator = adminCreateUserValidator;
        _configuration = configuration;
        _environment = environment;
        _logger = logger;
    }

    public async Task<LoginResponseDto> RegisterAsync(
        RegisterRequestDto request,
        int? requestingUserId,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_registerValidator, request, cancellationToken);

        var roleName = request.RoleName;
        if (!string.Equals(roleName, AppRoles.Patient, StringComparison.OrdinalIgnoreCase))
        {
            if (requestingUserId is null)
                throw new UnauthorizedException("Only administrators can register staff accounts.");

            var admin = await _userRepository.GetByIdAsync(requestingUserId.Value, cancellationToken);
            if (admin is null || !admin.UserRoles.Any(ur => ur.Role.RoleName == AppRoles.Admin))
                throw new UnauthorizedException("Only administrators can assign non-patient roles.");
        }

        if (await _userRepository.UsernameExistsAsync(request.Username, cancellationToken))
            throw new ConflictException("Username is already taken.");

        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            throw new ConflictException("Email is already registered.");

        var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken)
            ?? throw new AppValidationException($"Role '{roleName}' is not valid.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        await _userRepository.AddUserRoleAsync(new UserRole
        {
            UserId = user.UserId,
            RoleId = role.RoleId,
            AssignedAt = DateTime.UtcNow
        }, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        user.UserRoles = [new UserRole { Role = role }];
        _logger.LogInformation("User {Username} registered with role {Role}", user.Username, roleName);

        return _jwtTokenService.GenerateToken(MapToProfile(user));
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            throw new AppValidationException("Username and password are required.");

        var user = await _userRepository.GetByUsernameAsync(request.Username, cancellationToken)
            ?? throw new UnauthorizedException("Invalid username or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid username or password.");

        _logger.LogInformation("User {Username} logged in", user.Username);
        return _jwtTokenService.GenerateToken(MapToProfile(user));
    }

    public Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        // JWT is stateless; client discards token. Server acknowledges logout.
        return Task.CompletedTask;
    }

    public async Task<UserProfileDto> GetProfileAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");
        return MapToProfile(user);
    }

    public async Task<UserProfileDto> UpdateProfileAsync(
        int userId,
        UpdateProfileRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateProfileValidator, request, cancellationToken);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase)
            && await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            throw new ConflictException("Email is already in use.");

        user.FullName = request.FullName;
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;

        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        return MapToProfile(user);
    }

    public async Task ChangePasswordAsync(
        int userId,
        ChangePasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_changePasswordValidator, request, cancellationToken);

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} changed password", userId);
    }

    public async Task<ForgotPasswordResponseDto> ForgotPasswordAsync(
        ForgotPasswordRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_forgotPasswordValidator, request, cancellationToken);

        var user = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);

        // Always return generic message to prevent email enumeration
        const string genericMessage = "If the email exists, a password reset link has been sent.";

        if (user is null)
            return new ForgotPasswordResponseDto { Message = genericMessage };

        await _resetTokenRepository.InvalidateUserTokensAsync(user.UserId, cancellationToken);

        var expiryHours = int.Parse(_configuration["AuthSettings:PasswordResetExpiryHours"] ?? "1");
        var expiresAt = DateTime.UtcNow.AddHours(expiryHours);
        var tokenValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

        var resetToken = new PasswordResetToken
        {
            UserId = user.UserId,
            Token = tokenValue,
            ExpiresAt = expiresAt,
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _resetTokenRepository.CreateAsync(resetToken, cancellationToken);
        await _resetTokenRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset token created for user {UserId}", user.UserId);

        // In production, send token via email. Expose in dev only.
        return new ForgotPasswordResponseDto
        {
            Message = genericMessage,
            ResetToken = _environment.IsDevelopment() ? tokenValue : null,
            ExpiresAt = _environment.IsDevelopment() ? expiresAt : null
        };
    }

    public async Task ResetPasswordAsync(ResetPasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_resetPasswordValidator, request, cancellationToken);

        var resetToken = await _resetTokenRepository.GetValidTokenAsync(request.Token, cancellationToken)
            ?? throw new AppValidationException("Invalid or expired reset token.");

        resetToken.User.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepository.UpdateAsync(resetToken.User, cancellationToken);
        await _resetTokenRepository.MarkAsUsedAsync(resetToken, cancellationToken);
        await _resetTokenRepository.SaveChangesAsync(cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Password reset completed for user {UserId}", resetToken.UserId);
    }

    public async Task<UserProfileDto> AdminCreateUserAsync(
        AdminCreateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_adminCreateUserValidator, request, cancellationToken);

        var registerRequest = new RegisterRequestDto
        {
            Username = request.Username,
            Email = request.Email,
            Password = request.Password,
            ConfirmPassword = request.Password,
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            RoleName = request.RoleName
        };

        await ValidateAsync(_registerValidator, registerRequest, cancellationToken);
        return await CreateUserWithRoleAsync(registerRequest, request.RoleName, cancellationToken);
    }

    private async Task<UserProfileDto> CreateUserWithRoleAsync(
        RegisterRequestDto request,
        string roleName,
        CancellationToken cancellationToken)
    {
        if (await _userRepository.UsernameExistsAsync(request.Username, cancellationToken))
            throw new ConflictException("Username is already taken.");

        if (await _userRepository.EmailExistsAsync(request.Email, cancellationToken))
            throw new ConflictException("Email is already registered.");

        var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken)
            ?? throw new AppValidationException($"Role '{roleName}' is not valid.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FullName = request.FullName,
            PhoneNumber = request.PhoneNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        await _userRepository.AddUserRoleAsync(new UserRole
        {
            UserId = user.UserId,
            RoleId = role.RoleId,
            AssignedAt = DateTime.UtcNow
        }, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        user.UserRoles = [new UserRole { Role = role }];
        return MapToProfile(user);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
            throw new AppValidationException("Validation failed.", result.Errors.Select(e => e.ErrorMessage));
    }

    private static UserProfileDto MapToProfile(User user) => new()
    {
        UserId = user.UserId,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        PhoneNumber = user.PhoneNumber,
        Roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
        CreatedAt = user.CreatedAt
    };
}
