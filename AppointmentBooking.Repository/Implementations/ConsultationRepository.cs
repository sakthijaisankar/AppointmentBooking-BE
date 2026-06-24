using AppointmentBooking.Database;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Repository.Implementations;

public class ConsultationRepository : IConsultationRepository
{
    private readonly AppointmentBookingDbContext _context;

    public ConsultationRepository(AppointmentBookingDbContext context)
    {
        _context = context;
    }

    public async Task<Consultation?> GetByIdAsync(int consultationId, CancellationToken cancellationToken = default)
    {
        return await _context.Consultations
            .Include(c => c.Appointment)
                .ThenInclude(a => a.AppointmentStatus)
            .Include(c => c.Doctor)
                .ThenInclude(d => d.Specialization)
            .Include(c => c.Patient)
            .Include(c => c.ConsultedByUser)
            .Include(c => c.Prescriptions)
            .FirstOrDefaultAsync(c => c.ConsultationId == consultationId, cancellationToken);
    }

    public async Task<Consultation?> GetByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        return await _context.Consultations
            .Include(c => c.Appointment)
                .ThenInclude(a => a.AppointmentStatus)
            .Include(c => c.Doctor)
                .ThenInclude(d => d.Specialization)
            .Include(c => c.Patient)
            .Include(c => c.ConsultedByUser)
            .Include(c => c.Prescriptions)
            .FirstOrDefaultAsync(c => c.AppointmentId == appointmentId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Consultation> Items, int TotalCount)> GetByPatientIdAsync(int patientId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Consultations
            .Include(c => c.Appointment)
            .Include(c => c.Doctor)
                .ThenInclude(d => d.Specialization)
            .Include(c => c.Prescriptions)
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<Consultation> Items, int TotalCount)> GetByDoctorIdAsync(int doctorId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Consultations
            .Include(c => c.Appointment)
            .Include(c => c.Patient)
            .Include(c => c.Prescriptions)
            .Where(c => c.DoctorId == doctorId)
            .OrderByDescending(c => c.CreatedAt);

        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<Prescription?> GetPrescriptionByIdAsync(int prescriptionId, CancellationToken cancellationToken = default)
    {
        return await _context.Prescriptions
            .Include(p => p.Consultation)
            .FirstOrDefaultAsync(p => p.PrescriptionId == prescriptionId, cancellationToken);
    }

    public async Task AddAsync(Consultation consultation, CancellationToken cancellationToken = default)
    {
        await _context.Consultations.AddAsync(consultation, cancellationToken);
    }

    public async Task AddPrescriptionAsync(Prescription prescription, CancellationToken cancellationToken = default)
    {
        await _context.Prescriptions.AddAsync(prescription, cancellationToken);
    }

    public Task RemovePrescriptionAsync(Prescription prescription, CancellationToken cancellationToken = default)
    {
        _context.Prescriptions.Remove(prescription);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
