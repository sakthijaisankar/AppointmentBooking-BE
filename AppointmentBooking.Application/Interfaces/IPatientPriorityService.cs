using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.DTOs.PatientPriority;

namespace AppointmentBooking.Application.Interfaces;

public interface IPatientPriorityService
{
    Task<PatientPriorityClassificationDto> ClassifyPatientAsync(
        int patientId,
        ClassifyPatientPriorityRequestDto request,
        int? userId,
        CancellationToken cancellationToken = default);

    Task<PatientPriorityClassificationDto?> GetCurrentClassificationAsync(
        int patientId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<PatientPriorityClassificationDto>> GetClassificationHistoryAsync(
        int patientId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PatientPriorityClassificationDto> OverrideClassificationAsync(
        int classificationId,
        OverridePatientPriorityRequestDto request,
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PriorityLevelDto>> GetPriorityLevelsAsync(CancellationToken cancellationToken = default);

    Task<MlModelVersionDto?> GetActiveModelVersionAsync(CancellationToken cancellationToken = default);
}
