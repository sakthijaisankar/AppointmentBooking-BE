using AppointmentBooking.Database.Entities;

namespace AppointmentBooking.Repository.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(int patientId, CancellationToken cancellationToken = default);
    Task<Patient?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Patient?> GetDetailByIdAsync(int patientId, CancellationToken cancellationToken = default);
    Task<Patient?> GetDetailByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Patient> Items, int TotalCount)> GetPagedAsync(string? search, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<bool> ExistsByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<string> GeneratePatientCodeAsync(CancellationToken cancellationToken = default);
    Task<Patient> CreateAsync(Patient patient, CancellationToken cancellationToken = default);
    Task UpdateAsync(Patient patient, CancellationToken cancellationToken = default);

    Task<EmergencyContact?> GetEmergencyContactByIdAsync(int contactId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmergencyContact>> GetEmergencyContactsAsync(int patientId, CancellationToken cancellationToken = default);
    Task<EmergencyContact> AddEmergencyContactAsync(EmergencyContact contact, CancellationToken cancellationToken = default);
    Task UpdateEmergencyContactAsync(EmergencyContact contact, CancellationToken cancellationToken = default);
    Task DeleteEmergencyContactAsync(EmergencyContact contact, CancellationToken cancellationToken = default);

    Task<PatientMedicalHistory?> GetMedicalHistoryByIdAsync(int historyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientMedicalHistory>> GetMedicalHistoryAsync(int patientId, CancellationToken cancellationToken = default);
    Task<PatientMedicalHistory> AddMedicalHistoryAsync(PatientMedicalHistory history, CancellationToken cancellationToken = default);
    Task UpdateMedicalHistoryAsync(PatientMedicalHistory history, CancellationToken cancellationToken = default);
    Task DeleteMedicalHistoryAsync(PatientMedicalHistory history, CancellationToken cancellationToken = default);

    Task<PatientDocument?> GetDocumentByIdAsync(int documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientDocument>> GetDocumentsAsync(int patientId, CancellationToken cancellationToken = default);
    Task<PatientDocument> AddDocumentAsync(PatientDocument document, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(PatientDocument document, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
