using AppointmentBooking.Database;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Repository.Implementations;

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AppointmentBookingDbContext _context;

    public PasswordResetTokenRepository(AppointmentBookingDbContext context)
    {
        _context = context;
    }

    public Task<PasswordResetToken?> GetValidTokenAsync(string token, CancellationToken cancellationToken = default) =>
        _context.PasswordResetTokens
            .Include(t => t.User)
            .ThenInclude(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(
                t => t.Token == token && !t.IsUsed && t.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

    public async Task InvalidateUserTokensAsync(int userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == userId && !t.IsUsed)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.IsUsed = true;
            token.UsedAt = DateTime.UtcNow;
        }
    }

    public async Task<PasswordResetToken> CreateAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        await _context.PasswordResetTokens.AddAsync(token, cancellationToken);
        return token;
    }

    public Task MarkAsUsedAsync(PasswordResetToken token, CancellationToken cancellationToken = default)
    {
        token.IsUsed = true;
        token.UsedAt = DateTime.UtcNow;
        _context.PasswordResetTokens.Update(token);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
