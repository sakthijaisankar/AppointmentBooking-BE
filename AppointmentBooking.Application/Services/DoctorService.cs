using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.Constants;
using AppointmentBooking.Application.DTOs.Doctors;
using AppValidationException = AppointmentBooking.Application.Exceptions.ValidationException;
using AppointmentBooking.Application.Exceptions;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBooking.Application.Services;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly ISpecializationRepository _specializationRepository;
    private readonly IUserRepository _userRepository;
    
    private readonly IValidator<CreateDoctorProfileRequestDto> _createProfileValidator;
    private readonly IValidator<UpdateDoctorProfileRequestDto> _updateProfileValidator;
    private readonly IValidator<CreateDoctorScheduleRequestDto> _createScheduleValidator;
    private readonly IValidator<CreateSpecializationRequestDto> _createSpecializationValidator;
    private readonly IValidator<UpdateSpecializationRequestDto> _updateSpecializationValidator;

    public DoctorService(
        IDoctorRepository doctorRepository,
        ISpecializationRepository specializationRepository,
        IUserRepository userRepository,
        IValidator<CreateDoctorProfileRequestDto> createProfileValidator,
        IValidator<UpdateDoctorProfileRequestDto> updateProfileValidator,
        IValidator<CreateDoctorScheduleRequestDto> createScheduleValidator,
        IValidator<CreateSpecializationRequestDto> createSpecializationValidator,
        IValidator<UpdateSpecializationRequestDto> updateSpecializationValidator)
    {
        _doctorRepository = doctorRepository;
        _specializationRepository = specializationRepository;
        _userRepository = userRepository;
        _createProfileValidator = createProfileValidator;
        _updateProfileValidator = updateProfileValidator;
        _createScheduleValidator = createScheduleValidator;
        _createSpecializationValidator = createSpecializationValidator;
        _updateSpecializationValidator = updateSpecializationValidator;
    }

    // Doctor Profile Management
    public async Task<DoctorDetailDto> CreateProfileAsync(CreateDoctorProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createProfileValidator, request, cancellationToken);

        // Ensure user exists and has Doctor role
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        if (!user.UserRoles.Any(ur => ur.Role.RoleName == AppRoles.Doctor))
            throw new UnauthorizedException("Only users with the Doctor role can have a doctor profile.");

        // Check if doctor profile already exists
        if (await _doctorRepository.ExistsByUserIdAsync(request.UserId, cancellationToken))
            throw new ConflictException("Doctor profile already exists for this user.");

        // Ensure specialization exists
        var spec = await _specializationRepository.GetByIdAsync(request.SpecializationId, cancellationToken)
            ?? throw new NotFoundException("Specialization not found.");

        var doctor = new Doctor
        {
            ClinicId = request.ClinicId,
            UserId = request.UserId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            SpecializationId = request.SpecializationId,
            LicenseNumber = request.LicenseNumber,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _doctorRepository.CreateAsync(doctor, cancellationToken);
        await _doctorRepository.SaveChangesAsync(cancellationToken);

        var details = await _doctorRepository.GetDetailByIdAsync(doctor.DoctorId, cancellationToken);
        return MapToDetail(details!);
    }

    public async Task<DoctorDetailDto> UpdateProfileAsync(int doctorId, UpdateDoctorProfileRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateProfileValidator, request, cancellationToken);

        var doctor = await _doctorRepository.GetByIdAsync(doctorId, cancellationToken)
            ?? throw new NotFoundException("Doctor profile not found.");

        // Ensure specialization exists
        var spec = await _specializationRepository.GetByIdAsync(request.SpecializationId, cancellationToken)
            ?? throw new NotFoundException("Specialization not found.");

        doctor.FirstName = request.FirstName;
        doctor.LastName = request.LastName;
        doctor.SpecializationId = request.SpecializationId;
        doctor.LicenseNumber = request.LicenseNumber;

        await _doctorRepository.UpdateAsync(doctor, cancellationToken);
        await _doctorRepository.SaveChangesAsync(cancellationToken);

        var details = await _doctorRepository.GetDetailByIdAsync(doctorId, cancellationToken);
        return MapToDetail(details!);
    }

    public async Task<DoctorDetailDto> GetByIdAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await _doctorRepository.GetDetailByIdAsync(doctorId, cancellationToken)
            ?? throw new NotFoundException("Doctor not found.");
        return MapToDetail(doctor);
    }

    public async Task<DoctorDetailDto?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        var doctor = await _doctorRepository.GetByUserIdAsync(userId, cancellationToken);
        if (doctor == null) return null;

        var details = await _doctorRepository.GetDetailByIdAsync(doctor.DoctorId, cancellationToken);
        return MapToDetail(details!);
    }

    public async Task<PagedResult<DoctorProfileDto>> GetAllAsync(string? search, int? specializationId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var (items, total) = await _doctorRepository.GetPagedAsync(search, specializationId, pageNumber, pageSize, cancellationToken);
        
        return new PagedResult<DoctorProfileDto>
        {
            Items = items.Select(MapToProfileDto),
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task DeleteProfileAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        var doctor = await _doctorRepository.GetByIdAsync(doctorId, cancellationToken)
            ?? throw new NotFoundException("Doctor not found.");

        doctor.IsActive = false;
        await _doctorRepository.UpdateAsync(doctor, cancellationToken);
        await _doctorRepository.SaveChangesAsync(cancellationToken);
    }

    // Doctor Schedule Management
    public async Task<DoctorScheduleDto> AddScheduleAsync(int doctorId, CreateDoctorScheduleRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createScheduleValidator, request, cancellationToken);

        var doctor = await _doctorRepository.GetByIdAsync(doctorId, cancellationToken)
            ?? throw new NotFoundException("Doctor profile not found.");

        var startTime = TimeSpan.Parse(request.StartTime);
        var endTime = TimeSpan.Parse(request.EndTime);

        // Check for schedule overlaps
        var existingSchedules = await _doctorRepository.GetSchedulesByDoctorIdAsync(doctorId, cancellationToken);
        var daySchedules = existingSchedules.Where(s => s.DayOfWeek == request.DayOfWeek);
        foreach (var s in daySchedules)
        {
            if (startTime < s.EndTime && s.StartTime < endTime)
            {
                throw new ConflictException($"Schedule overlaps with existing timeframe: {s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm} on {GetDayName(s.DayOfWeek)}.");
            }
        }

        var schedule = new DoctorSchedule
        {
            DoctorId = doctorId,
            DayOfWeek = request.DayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            SlotDurationMinutes = request.SlotDurationMinutes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _doctorRepository.AddScheduleAsync(schedule, cancellationToken);
        await _doctorRepository.SaveChangesAsync(cancellationToken);

        return MapSchedule(schedule);
    }

    public async Task<DoctorScheduleDto> UpdateScheduleAsync(int scheduleId, UpdateDoctorScheduleRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createScheduleValidator, request, cancellationToken);

        var schedule = await _doctorRepository.GetScheduleByIdAsync(scheduleId, cancellationToken)
            ?? throw new NotFoundException("Schedule not found.");

        var startTime = TimeSpan.Parse(request.StartTime);
        var endTime = TimeSpan.Parse(request.EndTime);

        // Check for overlaps, excluding the current schedule being updated
        var existingSchedules = await _doctorRepository.GetSchedulesByDoctorIdAsync(schedule.DoctorId, cancellationToken);
        var daySchedules = existingSchedules.Where(s => s.DayOfWeek == request.DayOfWeek && s.DoctorScheduleId != scheduleId);
        foreach (var s in daySchedules)
        {
            if (startTime < s.EndTime && s.StartTime < endTime)
            {
                throw new ConflictException($"Schedule overlaps with existing timeframe: {s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm} on {GetDayName(s.DayOfWeek)}.");
            }
        }

        schedule.DayOfWeek = request.DayOfWeek;
        schedule.StartTime = startTime;
        schedule.EndTime = endTime;
        schedule.SlotDurationMinutes = request.SlotDurationMinutes;
        schedule.UpdatedAt = DateTime.UtcNow;

        await _doctorRepository.UpdateScheduleAsync(schedule, cancellationToken);
        await _doctorRepository.SaveChangesAsync(cancellationToken);

        return MapSchedule(schedule);
    }

    public async Task<IReadOnlyList<DoctorScheduleDto>> GetSchedulesAsync(int doctorId, CancellationToken cancellationToken = default)
    {
        // Ensure doctor exists
        _ = await _doctorRepository.GetByIdAsync(doctorId, cancellationToken)
            ?? throw new NotFoundException("Doctor not found.");

        var items = await _doctorRepository.GetSchedulesByDoctorIdAsync(doctorId, cancellationToken);
        return items.Select(MapSchedule).ToList();
    }

    public async Task DeleteScheduleAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _doctorRepository.GetScheduleByIdAsync(scheduleId, cancellationToken)
            ?? throw new NotFoundException("Schedule not found.");

        await _doctorRepository.DeleteScheduleAsync(schedule, cancellationToken);
        await _doctorRepository.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> GetDoctorIdByScheduleIdAsync(int scheduleId, CancellationToken cancellationToken = default)
    {
        var schedule = await _doctorRepository.GetScheduleByIdAsync(scheduleId, cancellationToken)
            ?? throw new NotFoundException("Schedule not found.");
        return schedule.DoctorId;
    }

    // Specializations Master Management
    public async Task<SpecializationDto> CreateSpecializationAsync(CreateSpecializationRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_createSpecializationValidator, request, cancellationToken);

        if (await _specializationRepository.GetByNameAsync(request.SpecializationName, cancellationToken) != null)
            throw new ConflictException("Specialization with this name already exists.");

        var specialization = new Specialization
        {
            SpecializationName = request.SpecializationName,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _specializationRepository.CreateAsync(specialization, cancellationToken);
        await _specializationRepository.SaveChangesAsync(cancellationToken);

        return MapSpecialization(specialization);
    }

    public async Task<SpecializationDto> UpdateSpecializationAsync(int specializationId, UpdateSpecializationRequestDto request, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_updateSpecializationValidator, request, cancellationToken);

        var specialization = await _specializationRepository.GetByIdAsync(specializationId, cancellationToken)
            ?? throw new NotFoundException("Specialization not found.");

        var matched = await _specializationRepository.GetByNameAsync(request.SpecializationName, cancellationToken);
        if (matched != null && matched.SpecializationId != specializationId)
            throw new ConflictException("Specialization with this name already exists.");

        specialization.SpecializationName = request.SpecializationName;
        specialization.Description = request.Description;
        specialization.IsActive = request.IsActive;

        await _specializationRepository.UpdateAsync(specialization, cancellationToken);
        await _specializationRepository.SaveChangesAsync(cancellationToken);

        return MapSpecialization(specialization);
    }

    public async Task<IReadOnlyList<SpecializationDto>> GetSpecializationsAsync(CancellationToken cancellationToken = default)
    {
        var items = await _specializationRepository.GetAllActiveAsync(cancellationToken);
        return items.Select(MapSpecialization).ToList();
    }

    public async Task DeleteSpecializationAsync(int specializationId, CancellationToken cancellationToken = default)
    {
        var specialization = await _specializationRepository.GetByIdAsync(specializationId, cancellationToken)
            ?? throw new NotFoundException("Specialization not found.");

        specialization.IsActive = false;
        await _specializationRepository.UpdateAsync(specialization, cancellationToken);
        await _specializationRepository.SaveChangesAsync(cancellationToken);
    }

    // Helper utilities
    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
            throw new AppValidationException("Validation failed.", result.Errors.Select(e => e.ErrorMessage));
    }

    private static string GetDayName(int dayOfWeek)
    {
        return ((DayOfWeek)dayOfWeek).ToString();
    }

    private static DoctorProfileDto MapToProfileDto(Doctor d) => new()
    {
        DoctorId = d.DoctorId,
        ClinicId = d.ClinicId,
        UserId = d.UserId,
        FirstName = d.FirstName,
        LastName = d.LastName,
        FullName = $"Dr. {d.FirstName} {d.LastName}",
        SpecializationId = d.SpecializationId,
        SpecializationName = d.Specialization?.SpecializationName ?? "Unknown",
        LicenseNumber = d.LicenseNumber,
        IsActive = d.IsActive,
        CreatedAt = d.CreatedAt
    };

    private static DoctorDetailDto MapToDetail(Doctor d) => new()
    {
        DoctorId = d.DoctorId,
        ClinicId = d.ClinicId,
        UserId = d.UserId,
        FirstName = d.FirstName,
        LastName = d.LastName,
        FullName = $"Dr. {d.FirstName} {d.LastName}",
        SpecializationId = d.SpecializationId,
        SpecializationName = d.Specialization?.SpecializationName ?? "Unknown",
        LicenseNumber = d.LicenseNumber,
        IsActive = d.IsActive,
        CreatedAt = d.CreatedAt,
        ClinicName = d.Clinic?.ClinicName ?? "Unknown",
        Schedules = d.Schedules?.Select(MapSchedule).ToList() ?? new List<DoctorScheduleDto>()
    };

    private static DoctorScheduleDto MapSchedule(DoctorSchedule s) => new()
    {
        DoctorScheduleId = s.DoctorScheduleId,
        DoctorId = s.DoctorId,
        DayOfWeek = s.DayOfWeek,
        DayName = GetDayName(s.DayOfWeek),
        StartTime = s.StartTime.ToString(@"hh\:mm"),
        EndTime = s.EndTime.ToString(@"hh\:mm"),
        SlotDurationMinutes = s.SlotDurationMinutes
    };

    private static SpecializationDto MapSpecialization(Specialization s) => new()
    {
        SpecializationId = s.SpecializationId,
        SpecializationName = s.SpecializationName,
        Description = s.Description,
        IsActive = s.IsActive
    };
}
