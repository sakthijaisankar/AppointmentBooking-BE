using AppointmentBooking.Database.Entities;

namespace AppointmentBooking.Repository.Interfaces;

public interface IDoctorRepository
{
    Task<Doctor?> GetByIdAsync(int doctorId, CancellationToken cancellationToken = default);
    Task<Doctor?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Doctor?> GetDetailByIdAsync(int doctorId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Doctor> Items, int TotalCount)> GetPagedAsync(
        string? search, int? specializationId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Doctor> CreateAsync(Doctor doctor, CancellationToken cancellationToken = default);
    Task UpdateAsync(Doctor doctor, CancellationToken cancellationToken = default);
    
    // Doctor Schedules
    Task<DoctorSchedule?> GetScheduleByIdAsync(int scheduleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorSchedule>> GetSchedulesByDoctorIdAsync(int doctorId, CancellationToken cancellationToken = default);
    Task<DoctorSchedule> AddScheduleAsync(DoctorSchedule schedule, CancellationToken cancellationToken = default);
    Task UpdateScheduleAsync(DoctorSchedule schedule, CancellationToken cancellationToken = default);
    Task DeleteScheduleAsync(DoctorSchedule schedule, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
