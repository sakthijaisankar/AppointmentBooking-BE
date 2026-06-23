using System;
using System.Collections.Generic;

namespace AppointmentBooking.Application.DTOs.Symptom;

public record SymptomDto
{
    public int SymptomId { get; init; }
    public string SymptomName { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record SymptomSubmissionItemDto
{
    public int SymptomId { get; init; }
    public int SeverityLevel { get; init; }
    public string? Notes { get; init; }
}

public record SubmitSymptomsRequestDto
{
    public int AppointmentId { get; init; }
    public string? ExistingConditions { get; init; }
    public string? Notes { get; init; }
    public List<SymptomSubmissionItemDto> Symptoms { get; init; } = new();
}

public record PatientSymptomDetailDto
{
    public int PatientSymptomId { get; init; }
    public int SymptomId { get; init; }
    public string SymptomName { get; init; } = string.Empty;
    public int SeverityLevel { get; init; }
    public string? Notes { get; init; }
}

public record AppointmentSymptomsDetailDto
{
    public int AppointmentId { get; init; }
    public string AppointmentNumber { get; init; } = string.Empty;
    public int PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string? ExistingConditions { get; init; }
    public string? SubmissionNotes { get; init; }
    public List<PatientSymptomDetailDto> Symptoms { get; init; } = new();
}
