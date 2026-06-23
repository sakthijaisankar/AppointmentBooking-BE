using AppointmentBooking.Database;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBooking.Repository.Implementations;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly AppointmentBookingDbContext _context;

    public AppointmentRepository(AppointmentBookingDbContext context)
    {
        _context = context;
    }

    public Task<Appointment?> GetByIdAsync(int appointmentId, CancellationToken cancellationToken = default) =>
        _context.Appointments
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId, cancellationToken);

    public Task<Appointment?> GetDetailByIdAsync(int appointmentId, CancellationToken cancellationToken = default) =>
        _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .ThenInclude(d => d.Specialization)
            .Include(a => a.Clinic)
            .Include(a => a.AppointmentStatus)
            .Include(a => a.CurrentPriorityClassification)
            .ThenInclude(c => c.PredictedPriorityLevel)
            .FirstOrDefaultAsync(a => a.AppointmentId == appointmentId, cancellationToken);

    public async Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(
        string? search, int? statusId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor)
            .ThenInclude(d => d.Specialization)
            .Include(a => a.Clinic)
            .Include(a => a.AppointmentStatus)
            .Include(a => a.CurrentPriorityClassification)
            .ThenInclude(c => c.PredictedPriorityLevel)
            .AsQueryable();

        if (statusId.HasValue)
        {
            query = query.Where(a => a.AppointmentStatusId == statusId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(a => 
                a.AppointmentNumber.ToLower().Contains(term) ||
                a.Patient.FirstName.ToLower().Contains(term) ||
                a.Patient.LastName.ToLower().Contains(term) ||
                (a.Patient.FirstName + " " + a.Patient.LastName).ToLower().Contains(term) ||
                a.Doctor.FirstName.ToLower().Contains(term) ||
                a.Doctor.LastName.ToLower().Contains(term) ||
                (a.Doctor.FirstName + " " + a.Doctor.LastName).ToLower().Contains(term)
            );
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.ScheduledDateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<Appointment>> GetActiveAppointmentsByPatientIdAsync(int patientId, CancellationToken cancellationToken = default) =>
        await _context.Appointments
            .Include(a => a.Doctor)
            .ThenInclude(d => d.Specialization)
            .Include(a => a.Clinic)
            .Include(a => a.AppointmentStatus)
            .Include(a => a.CurrentPriorityClassification)
            .ThenInclude(c => c.PredictedPriorityLevel)
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.ScheduledDateTime)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetAppointmentsByDoctorIdAsync(int doctorId, CancellationToken cancellationToken = default) =>
        await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.AppointmentStatus)
            .Include(a => a.CurrentPriorityClassification)
            .ThenInclude(c => c.PredictedPriorityLevel)
            .Where(a => a.DoctorId == doctorId)
            .OrderByDescending(a => a.ScheduledDateTime)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetAppointmentsByDoctorAndDateAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var startDateTime = date.ToDateTime(TimeOnly.MinValue);
        var endDateTime = date.ToDateTime(TimeOnly.MaxValue);

        return await _context.Appointments
            .Include(a => a.AppointmentStatus)
            .Where(a => a.DoctorId == doctorId 
                     && a.ScheduledDateTime >= startDateTime 
                     && a.ScheduledDateTime <= endDateTime
                     && a.AppointmentStatus.StatusName != "Cancelled")
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetAppointmentCountForDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var startDateTime = date.ToDateTime(TimeOnly.MinValue);
        var endDateTime = date.ToDateTime(TimeOnly.MaxValue);

        return await _context.Appointments
            .CountAsync(a => a.ScheduledDateTime >= startDateTime 
                          && a.ScheduledDateTime <= endDateTime, cancellationToken);
    }

    // Status lookups
    public Task<AppointmentStatus?> GetStatusByNameAsync(string statusName, CancellationToken cancellationToken = default) =>
        _context.AppointmentStatuses.FirstOrDefaultAsync(s => s.StatusName == statusName, cancellationToken);

    public Task<AppointmentStatus?> GetStatusByIdAsync(int statusId, CancellationToken cancellationToken = default) =>
        _context.AppointmentStatuses.FirstOrDefaultAsync(s => s.AppointmentStatusId == statusId, cancellationToken);

    public async Task<IReadOnlyList<AppointmentStatus>> GetStatusesAsync(CancellationToken cancellationToken = default) =>
        await _context.AppointmentStatuses.ToListAsync(cancellationToken);

    public async Task<Appointment> CreateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        await _context.Appointments.AddAsync(appointment, cancellationToken);
        return appointment;
    }

    public Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default)
    {
        _context.Appointments.Update(appointment);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
