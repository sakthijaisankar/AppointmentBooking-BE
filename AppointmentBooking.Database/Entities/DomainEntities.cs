namespace AppointmentBooking.Database.Entities;

public class Role
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    public Patient? Patient { get; set; }
}

public class PasswordResetToken
{
    public int PasswordResetTokenId { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UsedAt { get; set; }

    public User User { get; set; } = null!;
}

public class UserRole
{
    public int UserRoleId { get; set; }
    public int UserId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}

public class Patient
{
    public int PatientId { get; set; }
    public int UserId { get; set; }
    public string PatientCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? BloodGroup { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<PatientMedicalHistory> MedicalHistory { get; set; } = new List<PatientMedicalHistory>();
    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = new List<EmergencyContact>();
    public ICollection<PatientDocument> Documents { get; set; } = new List<PatientDocument>();
    public ICollection<PatientClinicalFeature> ClinicalFeatures { get; set; } = new List<PatientClinicalFeature>();
    public ICollection<PatientPriorityClassification> PriorityClassifications { get; set; } = new List<PatientPriorityClassification>();
}

public class PatientMedicalHistory
{
    public int PatientMedicalHistoryId { get; set; }
    public int PatientId { get; set; }
    public string ConditionName { get; set; } = string.Empty;
    public DateOnly? DiagnosisDate { get; set; }
    public string? Description { get; set; }
    public bool IsChronic { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
}

public class EmergencyContact
{
    public int EmergencyContactId { get; set; }
    public int PatientId { get; set; }
    public string ContactName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Patient Patient { get; set; } = null!;
}

public class PatientDocument
{
    public int PatientDocumentId { get; set; }
    public int PatientId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; }
    public bool IsActive { get; set; }

    public Patient Patient { get; set; } = null!;
    public User UploadedByUser { get; set; } = null!;
}

public class Clinic
{
    public int ClinicId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}

public class Specialization
{
    public int SpecializationId { get; set; }
    public string SpecializationName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}

public class Doctor
{
    public int DoctorId { get; set; }
    public int ClinicId { get; set; }
    public int? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int SpecializationId { get; set; }
    public string LicenseNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public Clinic Clinic { get; set; } = null!;
    public User? User { get; set; }
    public Specialization Specialization { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();
}

public class DoctorSchedule
{
    public int DoctorScheduleId { get; set; }
    public int DoctorId { get; set; }
    public int DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int SlotDurationMinutes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Doctor Doctor { get; set; } = null!;
}

public class AppointmentStatus
{
    public int AppointmentStatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}

public class Appointment
{
    public int AppointmentId { get; set; }
    public string AppointmentNumber { get; set; } = string.Empty;
    public int PatientId { get; set; }
    public int DoctorId { get; set; }
    public int ClinicId { get; set; }
    public int AppointmentStatusId { get; set; }
    public DateTime ScheduledDateTime { get; set; }
    public string? ReasonForVisit { get; set; }
    public string? Notes { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? CurrentPriorityClassificationId { get; set; }

    public Patient Patient { get; set; } = null!;
    public Doctor Doctor { get; set; } = null!;
    public Clinic Clinic { get; set; } = null!;
    public AppointmentStatus AppointmentStatus { get; set; } = null!;
    public User? CreatedByUser { get; set; }
    public PatientPriorityClassification? CurrentPriorityClassification { get; set; }
    public QueueManagement? QueueManagement { get; set; }
    public ICollection<PatientSymptom> PatientSymptoms { get; set; } = new List<PatientSymptom>();
}

public class PriorityLevel
{
    public int PriorityLevelId { get; set; }
    public string LevelCode { get; set; } = string.Empty;
    public string LevelName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string ColorHex { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    public ICollection<PatientPriorityClassification> Classifications { get; set; } = new List<PatientPriorityClassification>();
}

public class MlModelVersion
{
    public int MlModelVersionId { get; set; }
    public string ModelName { get; set; } = string.Empty;
    public string VersionNumber { get; set; } = string.Empty;
    public string? ModelPath { get; set; }
    public string AlgorithmType { get; set; } = string.Empty;
    public decimal? AccuracyScore { get; set; }
    public bool IsActive { get; set; }
    public DateTime DeployedAt { get; set; }
    public string? Notes { get; set; }

    public ICollection<PatientPriorityClassification> Classifications { get; set; } = new List<PatientPriorityClassification>();
}

public class PatientClinicalFeature
{
    public int PatientClinicalFeatureId { get; set; }
    public int PatientId { get; set; }
    public int? AppointmentId { get; set; }
    public int Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public int? HeartRate { get; set; }
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public decimal? TemperatureCelsius { get; set; }
    public decimal? OxygenSaturation { get; set; }
    public int? PainLevel { get; set; }
    public int? SymptomSeverityScore { get; set; }
    public bool HasChronicCondition { get; set; }
    public bool HasRecentHospitalization { get; set; }
    public string? PrimarySymptoms { get; set; }
    public string? Comorbidities { get; set; }
    public DateTime CapturedAt { get; set; }
    public int? CapturedByUserId { get; set; }

    public Patient Patient { get; set; } = null!;
    public Appointment? Appointment { get; set; }
    public User? CapturedByUser { get; set; }
    public ICollection<PatientPriorityClassification> Classifications { get; set; } = new List<PatientPriorityClassification>();
}

public class PatientPriorityClassification
{
    public int PatientPriorityClassificationId { get; set; }
    public int PatientId { get; set; }
    public int PatientClinicalFeatureId { get; set; }
    public int MlModelVersionId { get; set; }
    public int PredictedPriorityLevelId { get; set; }
    public decimal ConfidenceScore { get; set; }
    public decimal RiskScore { get; set; }
    public string? ClassificationReason { get; set; }
    public string? InputFeaturesJson { get; set; }
    public bool IsCurrent { get; set; }
    public DateTime ClassifiedAt { get; set; }
    public int? ClassifiedByUserId { get; set; }

    public Patient Patient { get; set; } = null!;
    public PatientClinicalFeature PatientClinicalFeature { get; set; } = null!;
    public MlModelVersion MlModelVersion { get; set; } = null!;
    public PriorityLevel PredictedPriorityLevel { get; set; } = null!;
    public User? ClassifiedByUser { get; set; }
    public ICollection<PriorityClassificationOverride> Overrides { get; set; } = new List<PriorityClassificationOverride>();
    public ICollection<QueueManagement> QueueManagements { get; set; } = new List<QueueManagement>();
}

public class PriorityClassificationOverride
{
    public int PriorityClassificationOverrideId { get; set; }
    public int PatientPriorityClassificationId { get; set; }
    public int OriginalPriorityLevelId { get; set; }
    public int OverridePriorityLevelId { get; set; }
    public string OverrideReason { get; set; } = string.Empty;
    public int OverriddenByUserId { get; set; }
    public DateTime OverriddenAt { get; set; }

    public PatientPriorityClassification PatientPriorityClassification { get; set; } = null!;
    public PriorityLevel OriginalPriorityLevel { get; set; } = null!;
    public PriorityLevel OverridePriorityLevel { get; set; } = null!;
    public User OverriddenByUser { get; set; } = null!;
}

public class Symptom
{
    public int SymptomId { get; set; }
    public string SymptomName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PatientSymptom
{
    public int PatientSymptomId { get; set; }
    public int AppointmentId { get; set; }
    public int SymptomId { get; set; }
    public int SeverityLevel { get; set; }
    public string? ExistingConditions { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Appointment Appointment { get; set; } = null!;
    public Symptom Symptom { get; set; } = null!;
}

public class QueueStatus
{
    public int QueueStatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<QueueManagement> Queues { get; set; } = new List<QueueManagement>();
}

public class QueueManagement
{
    public int QueueId { get; set; }
    public int AppointmentId { get; set; }
    public int PatientPriorityClassificationId { get; set; }
    public string QueueNumber { get; set; } = string.Empty;
    public int QueueStatusId { get; set; }
    public int EstimatedWaitTimeMinutes { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CallingTime { get; set; }
    public DateTime? ConsultationStartTime { get; set; }
    public DateTime? ConsultationEndTime { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Appointment Appointment { get; set; } = null!;
    public PatientPriorityClassification PatientPriorityClassification { get; set; } = null!;
    public QueueStatus QueueStatus { get; set; } = null!;
}
