namespace AppointmentBooking.Application.DTOs.Consultation;

// ─── Request DTOs ───────────────────────────────────────────────────────────

public record CreatePrescriptionDto(
    string MedicineName,
    string Dosage,
    string Frequency,
    int DurationDays,
    string? Instructions
);

public record CreateConsultationRequestDto(
    int AppointmentId,
    string Diagnosis,
    string? ClinicalNotes,
    bool FollowUpRequired,
    DateOnly? FollowUpDate,
    List<CreatePrescriptionDto>? Prescriptions
);

public record UpdateConsultationRequestDto(
    string Diagnosis,
    string? ClinicalNotes,
    bool FollowUpRequired,
    DateOnly? FollowUpDate
);

public record AddPrescriptionRequestDto(
    string MedicineName,
    string Dosage,
    string Frequency,
    int DurationDays,
    string? Instructions
);

// ─── Response DTOs ──────────────────────────────────────────────────────────

public record PrescriptionDto(
    int PrescriptionId,
    int ConsultationId,
    string MedicineName,
    string Dosage,
    string Frequency,
    int DurationDays,
    string? Instructions,
    DateTime CreatedAt
);

public record ConsultationDto(
    int ConsultationId,
    int AppointmentId,
    string AppointmentNumber,
    int PatientId,
    string PatientName,
    string PatientCode,
    int DoctorId,
    string DoctorName,
    string SpecializationName,
    string Diagnosis,
    string? ClinicalNotes,
    bool FollowUpRequired,
    DateOnly? FollowUpDate,
    string? AppointmentStatusName,
    DateTime ScheduledDateTime,
    string? ConsultedByName,
    List<PrescriptionDto> Prescriptions,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record ConsultationSummaryDto(
    int ConsultationId,
    int AppointmentId,
    string AppointmentNumber,
    string PatientName,
    string PatientCode,
    string DoctorName,
    string Diagnosis,
    bool FollowUpRequired,
    DateOnly? FollowUpDate,
    int PrescriptionCount,
    DateTime CreatedAt
);
