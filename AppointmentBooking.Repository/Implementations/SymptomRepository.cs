using AppointmentBooking.Database;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBooking.Repository.Implementations;

public class SymptomRepository : ISymptomRepository
{
    private readonly AppointmentBookingDbContext _context;

    public SymptomRepository(AppointmentBookingDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Symptom>> GetActiveSymptomsAsync(CancellationToken cancellationToken = default) =>
        await _context.Symptoms
            .Where(s => s.IsActive)
            .OrderBy(s => s.SymptomName)
            .ToListAsync(cancellationToken);

    public Task<Symptom?> GetByIdAsync(int symptomId, CancellationToken cancellationToken = default) =>
        _context.Symptoms
            .FirstOrDefaultAsync(s => s.SymptomId == symptomId, cancellationToken);

    public async Task<IReadOnlyList<PatientSymptom>> GetSymptomsByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default) =>
        await _context.PatientSymptoms
            .Include(ps => ps.Symptom)
            .Where(ps => ps.AppointmentId == appointmentId)
            .ToListAsync(cancellationToken);

    public async Task AddPatientSymptomsAsync(IEnumerable<PatientSymptom> symptoms, CancellationToken cancellationToken = default)
    {
        await _context.PatientSymptoms.AddRangeAsync(symptoms, cancellationToken);
    }

    public Task RemovePatientSymptomsAsync(IEnumerable<PatientSymptom> symptoms, CancellationToken cancellationToken = default)
    {
        _context.PatientSymptoms.RemoveRange(symptoms);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
