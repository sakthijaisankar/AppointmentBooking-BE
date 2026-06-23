namespace AppointmentBooking.Application.DTOs.PatientPriority;

public record ClassifyPatientPriorityRequestDto
{
    public int? AppointmentId { get; init; }
    public int Age { get; init; }
    public string Gender { get; init; } = string.Empty;
    public int? HeartRate { get; init; }
    public int? BloodPressureSystolic { get; init; }
    public int? BloodPressureDiastolic { get; init; }
    public decimal? TemperatureCelsius { get; init; }
    public decimal? OxygenSaturation { get; init; }
    public int? PainLevel { get; init; }
    public int? SymptomSeverityScore { get; init; }
    public bool HasChronicCondition { get; init; }
    public bool HasRecentHospitalization { get; init; }
    public string? PrimarySymptoms { get; init; }
    public string? Comorbidities { get; init; }
}

public record OverridePatientPriorityRequestDto
{
    public int OverridePriorityLevelId { get; init; }
    public string OverrideReason { get; init; } = string.Empty;
}

public record PriorityLevelDto
{
    public int PriorityLevelId { get; init; }
    public string LevelCode { get; init; } = string.Empty;
    public string LevelName { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public string ColorHex { get; init; } = string.Empty;
    public string? Description { get; init; }
}

public record PatientPriorityClassificationDto
{
    public int PatientPriorityClassificationId { get; init; }
    public int PatientId { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public PriorityLevelDto PredictedPriorityLevel { get; init; } = null!;
    public PriorityLevelDto? EffectivePriorityLevel { get; init; }
    public decimal ConfidenceScore { get; init; }
    public decimal RiskScore { get; init; }
    public string? ClassificationReason { get; init; }
    public string ModelVersion { get; init; } = string.Empty;
    public bool IsCurrent { get; init; }
    public bool IsOverridden { get; init; }
    public string? OverrideReason { get; init; }
    public DateTime ClassifiedAt { get; init; }
    public ClassifyPatientPriorityRequestDto? ClinicalFeatures { get; init; }
}

public record MlModelVersionDto
{
    public int MlModelVersionId { get; init; }
    public string ModelName { get; init; } = string.Empty;
    public string VersionNumber { get; init; } = string.Empty;
    public string AlgorithmType { get; init; } = string.Empty;
    public decimal? AccuracyScore { get; init; }
    public bool IsActive { get; init; }
    public DateTime DeployedAt { get; init; }
}

public record PriorityPredictionResult
{
    public string PredictedLevelCode { get; init; } = string.Empty;
    public decimal ConfidenceScore { get; init; }
    public decimal RiskScore { get; init; }
    public string ClassificationReason { get; init; } = string.Empty;
    public string InputFeaturesJson { get; init; } = string.Empty;
}
