using AppointmentBooking.Database;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Repository.Implementations;

public class SpecializationRepository : ISpecializationRepository
{
    private readonly AppointmentBookingDbContext _context;

    public SpecializationRepository(AppointmentBookingDbContext context)
    {
        _context = context;
    }

    public Task<Specialization?> GetByIdAsync(int specializationId, CancellationToken cancellationToken = default) =>
        _context.Specializations.FirstOrDefaultAsync(s => s.SpecializationId == specializationId && s.IsActive, cancellationToken);

    public Task<Specialization?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        _context.Specializations.FirstOrDefaultAsync(s => s.SpecializationName == name && s.IsActive, cancellationToken);

    public async Task<IReadOnlyList<Specialization>> GetAllActiveAsync(CancellationToken cancellationToken = default) =>
        await _context.Specializations
            .Where(s => s.IsActive)
            .OrderBy(s => s.SpecializationName)
            .ToListAsync(cancellationToken);

    public async Task<Specialization> CreateAsync(Specialization specialization, CancellationToken cancellationToken = default)
    {
        await _context.Specializations.AddAsync(specialization, cancellationToken);
        return specialization;
    }

    public Task UpdateAsync(Specialization specialization, CancellationToken cancellationToken = default)
    {
        _context.Specializations.Update(specialization);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
