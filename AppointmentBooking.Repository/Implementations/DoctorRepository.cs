using AppointmentBooking.Database;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Repository.Implementations;

public class DoctorRepository : IDoctorRepository
{
    private readonly AppointmentBookingDbContext _context;

    public DoctorRepository(AppointmentBookingDbContext context)
    {
        _context = context;
    }

    public Task<Doctor?> GetByIdAsync(int doctorId, CancellationToken cancellationToken = default) =>
        _context.Doctors
            .Include(d => d.Specialization)
            .FirstOrDefaultAsync(d => d.DoctorId == doctorId && d.IsActive, cancellationToken);

    public Task<Doctor?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.Doctors
            .Include(d => d.Specialization)
            .FirstOrDefaultAsync(d => d.UserId == userId && d.IsActive, cancellationToken);

    public Task<Doctor?> GetDetailByIdAsync(int doctorId, CancellationToken cancellationToken = default) =>
        _context.Doctors
            .Include(d => d.Specialization)
            .Include(d => d.Schedules.Where(s => s.IsActive))
            .Include(d => d.Clinic)
            .FirstOrDefaultAsync(d => d.DoctorId == doctorId && d.IsActive, cancellationToken);

    public async Task<(IReadOnlyList<Doctor> Items, int TotalCount)> GetPagedAsync(
        string? search, int? specializationId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _context.Doctors
            .Include(d => d.Specialization)
            .Where(d => d.IsActive);

        if (specializationId.HasValue)
        {
            query = query.Where(d => d.SpecializationId == specializationId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(d => 
                d.FirstName.ToLower().Contains(term) || 
                d.LastName.ToLower().Contains(term) || 
                d.LicenseNumber.ToLower().Contains(term) ||
                (d.FirstName + " " + d.LastName).ToLower().Contains(term)
            );
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(d => d.LastName).ThenBy(d => d.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public Task<bool> ExistsByUserIdAsync(int userId, CancellationToken cancellationToken = default) =>
        _context.Doctors.AnyAsync(d => d.UserId == userId && d.IsActive, cancellationToken);

    public async Task<Doctor> CreateAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        await _context.Doctors.AddAsync(doctor, cancellationToken);
        return doctor;
    }

    public Task UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default)
    {
        _context.Doctors.Update(doctor);
        return Task.CompletedTask;
    }

    // Schedules
    public Task<DoctorSchedule?> GetScheduleByIdAsync(int scheduleId, CancellationToken cancellationToken = default) =>
        _context.DoctorSchedules
            .Include(s => s.Doctor)
            .FirstOrDefaultAsync(s => s.DoctorScheduleId == scheduleId && s.IsActive, cancellationToken);

    public async Task<IReadOnlyList<DoctorSchedule>> GetSchedulesByDoctorIdAsync(int doctorId, CancellationToken cancellationToken = default) =>
        await _context.DoctorSchedules
            .Where(s => s.DoctorId == doctorId && s.IsActive)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync(cancellationToken);

    public async Task<DoctorSchedule> AddScheduleAsync(DoctorSchedule schedule, CancellationToken cancellationToken = default)
    {
        await _context.DoctorSchedules.AddAsync(schedule, cancellationToken);
        return schedule;
    }

    public Task UpdateScheduleAsync(DoctorSchedule schedule, CancellationToken cancellationToken = default)
    {
        _context.DoctorSchedules.Update(schedule);
        return Task.CompletedTask;
    }

    public Task DeleteScheduleAsync(DoctorSchedule schedule, CancellationToken cancellationToken = default)
    {
        schedule.IsActive = false;
        schedule.UpdatedAt = DateTime.UtcNow;
        _context.DoctorSchedules.Update(schedule);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
