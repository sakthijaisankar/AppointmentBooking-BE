using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.DTOs.Consultation;

namespace AppointmentBooking.Application.Interfaces;

public interface IConsultationService
{
    Task<ConsultationDto> CreateConsultationAsync(CreateConsultationRequestDto request, int consultedByUserId, CancellationToken cancellationToken = default);
    Task<ConsultationDto?> GetByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task<ConsultationDto> GetByIdAsync(int consultationId, CancellationToken cancellationToken = default);
    Task<PagedResult<ConsultationSummaryDto>> GetPatientHistoryAsync(int patientId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<PagedResult<ConsultationSummaryDto>> GetDoctorConsultationsAsync(int doctorId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ConsultationDto> UpdateConsultationAsync(int consultationId, UpdateConsultationRequestDto request, int updatedByUserId, CancellationToken cancellationToken = default);
    Task<ConsultationDto> AddPrescriptionAsync(int consultationId, AddPrescriptionRequestDto request, CancellationToken cancellationToken = default);
    Task<ConsultationDto> DeletePrescriptionAsync(int consultationId, int prescriptionId, CancellationToken cancellationToken = default);
}
