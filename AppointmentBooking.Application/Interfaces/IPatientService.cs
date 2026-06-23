using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.DTOs.Patients;

namespace AppointmentBooking.Application.Interfaces;

public interface IPatientService
{
    Task<PatientDetailDto> CreateProfileAsync(int userId, CreatePatientProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<PatientDetailDto?> GetMyProfileAsync(int userId, CancellationToken cancellationToken = default);
    Task<PatientDetailDto> UpdateMyProfileAsync(int userId, UpdatePatientProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<PatientDetailDto> GetByIdAsync(int patientId, int requestingUserId, IReadOnlyList<string> roles, CancellationToken cancellationToken = default);
    Task<PagedResult<PatientListItemDto>> GetAllAsync(string? search, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<EmergencyContactDto> AddEmergencyContactAsync(int userId, IReadOnlyList<string> roles, int? patientId, CreateEmergencyContactRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmergencyContactDto>> GetEmergencyContactsAsync(int userId, IReadOnlyList<string> roles, int? patientId, CancellationToken cancellationToken = default);
    Task<EmergencyContactDto> UpdateEmergencyContactAsync(int userId, IReadOnlyList<string> roles, int contactId, UpdateEmergencyContactRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteEmergencyContactAsync(int userId, IReadOnlyList<string> roles, int contactId, CancellationToken cancellationToken = default);

    Task<PatientMedicalHistoryDto> AddMedicalHistoryAsync(int userId, IReadOnlyList<string> roles, int? patientId, CreateMedicalHistoryRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientMedicalHistoryDto>> GetMedicalHistoryAsync(int userId, IReadOnlyList<string> roles, int? patientId, CancellationToken cancellationToken = default);
    Task<PatientMedicalHistoryDto> UpdateMedicalHistoryAsync(int userId, IReadOnlyList<string> roles, int historyId, UpdateMedicalHistoryRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteMedicalHistoryAsync(int userId, IReadOnlyList<string> roles, int historyId, CancellationToken cancellationToken = default);

    Task<PatientDocumentDto> UploadDocumentAsync(int userId, IReadOnlyList<string> roles, int? patientId, UploadDocumentRequestDto metadata, Stream fileStream, string fileName, string contentType, long fileSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientDocumentDto>> GetDocumentsAsync(int userId, IReadOnlyList<string> roles, int? patientId, CancellationToken cancellationToken = default);
    Task<(Stream Stream, string ContentType, string FileName)> DownloadDocumentAsync(int userId, IReadOnlyList<string> roles, int documentId, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(int userId, IReadOnlyList<string> roles, int documentId, CancellationToken cancellationToken = default);
}
