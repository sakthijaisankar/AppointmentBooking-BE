using AppointmentBooking.Database.Entities;

namespace AppointmentBooking.Repository.Interfaces;

public interface IPatientPriorityRepository
{
    Task<Patient?> GetPatientByIdAsync(int patientId, CancellationToken cancellationToken = default);
    Task<Appointment?> GetAppointmentByIdAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PriorityLevel>> GetActivePriorityLevelsAsync(CancellationToken cancellationToken = default);
    Task<PriorityLevel?> GetPriorityLevelByIdAsync(int priorityLevelId, CancellationToken cancellationToken = default);
    Task<PriorityLevel?> GetPriorityLevelByCodeAsync(string levelCode, CancellationToken cancellationToken = default);
    Task<MlModelVersion?> GetActiveModelVersionAsync(CancellationToken cancellationToken = default);
    Task<PatientPriorityClassification?> GetCurrentClassificationAsync(int patientId, CancellationToken cancellationToken = default);
    Task<PatientPriorityClassification?> GetClassificationByIdAsync(int classificationId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientPriorityClassification>> GetClassificationHistoryAsync(int patientId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetClassificationHistoryCountAsync(int patientId, CancellationToken cancellationToken = default);
    Task<PatientClinicalFeature> AddClinicalFeaturesAsync(PatientClinicalFeature features, CancellationToken cancellationToken = default);
    Task<PatientPriorityClassification> AddClassificationAsync(PatientPriorityClassification classification, CancellationToken cancellationToken = default);
    Task<PriorityClassificationOverride> AddOverrideAsync(PriorityClassificationOverride overrideRecord, CancellationToken cancellationToken = default);
    Task SetCurrentClassificationAsync(int patientId, int newClassificationId, CancellationToken cancellationToken = default);
    Task LinkAppointmentToClassificationAsync(int appointmentId, int classificationId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
