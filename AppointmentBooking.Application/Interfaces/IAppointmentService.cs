using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.DTOs.Appointment;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBooking.Application.Interfaces;

public interface IAppointmentService
{
    Task<AppointmentDetailDto> BookAppointmentAsync(CreateAppointmentRequestDto request, int? createdByUserId, CancellationToken cancellationToken = default);
    Task<AppointmentDetailDto> UpdateAppointmentStatusAsync(int appointmentId, string statusName, string? notes, int? userId, CancellationToken cancellationToken = default);
    Task<AppointmentDetailDto> GetByIdAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task<PagedResult<AppointmentListItemDto>> GetPagedAsync(string? search, int? statusId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentListItemDto>> GetActiveAppointmentsByPatientIdAsync(int patientId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentListItemDto>> GetAppointmentsByDoctorIdAsync(int doctorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AvailableSlotDto>> GetAvailableSlotsAsync(int doctorId, DateOnly date, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentStatusDto>> GetStatusesAsync(CancellationToken cancellationToken = default);
}
