using AppointmentBooking.Database.Entities;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBooking.Repository.Interfaces;

public interface IAppointmentRepository
{
    Task<Appointment?> GetByIdAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task<Appointment?> GetDetailByIdAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Appointment> Items, int TotalCount)> GetPagedAsync(
        string? search, int? statusId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetActiveAppointmentsByPatientIdAsync(int patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetAppointmentsByDoctorIdAsync(int doctorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetAppointmentsByDoctorAndDateAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default);
    Task<int> GetAppointmentCountForDateAsync(DateOnly date, CancellationToken cancellationToken = default);
    
    // Status Lookup
    Task<AppointmentStatus?> GetStatusByNameAsync(string statusName, CancellationToken cancellationToken = default);
    Task<AppointmentStatus?> GetStatusByIdAsync(int statusId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentStatus>> GetStatusesAsync(CancellationToken cancellationToken = default);

    Task<Appointment> CreateAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
