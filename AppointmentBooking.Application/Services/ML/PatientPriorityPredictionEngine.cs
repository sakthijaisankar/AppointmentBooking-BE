using System.Text.Json;
using AppointmentBooking.Application.DTOs.PatientPriority;
using AppointmentBooking.Application.Interfaces.ML;

namespace AppointmentBooking.Application.Services.ML;

/// <summary>
/// Weighted clinical triage engine. Replace with ML.NET loaded model when trained model is available.
/// </summary>
public class PatientPriorityPredictionEngine : IPatientPriorityPredictionEngine
{
    public PriorityPredictionResult Predict(ClassifyPatientPriorityRequestDto input)
    {
        var riskScore = CalculateRiskScore(input);
        var (levelCode, confidence, reason) = MapRiskToPriority(riskScore, input);

        return new PriorityPredictionResult
        {
            PredictedLevelCode = levelCode,
            ConfidenceScore = Math.Round(confidence, 4),
            RiskScore = Math.Round(riskScore, 4),
            ClassificationReason = reason,
            InputFeaturesJson = JsonSerializer.Serialize(input)
        };
    }

    private static decimal CalculateRiskScore(ClassifyPatientPriorityRequestDto input)
    {
        decimal score = 0;

        if (input.Age >= 65) score += 15;
        else if (input.Age <= 5) score += 12;

        if (input.HeartRate is > 120 or < 50) score += 20;
        else if (input.HeartRate is > 100 or < 60) score += 10;

        if (input.BloodPressureSystolic is >= 180 or <= 90) score += 18;
        else if (input.BloodPressureSystolic is >= 140) score += 8;

        if (input.OxygenSaturation is < 90) score += 25;
        else if (input.OxygenSaturation is < 94) score += 12;

        if (input.TemperatureCelsius is >= 39.5m or <= 35.0m) score += 15;
        else if (input.TemperatureCelsius is >= 38.5m) score += 8;

        if (input.PainLevel is >= 8) score += 12;
        else if (input.PainLevel is >= 5) score += 6;

        if (input.SymptomSeverityScore is >= 8) score += 15;
        else if (input.SymptomSeverityScore is >= 5) score += 8;

        if (input.HasChronicCondition) score += 10;
        if (input.HasRecentHospitalization) score += 12;

        if (!string.IsNullOrWhiteSpace(input.PrimarySymptoms))
        {
            var symptoms = input.PrimarySymptoms.ToLowerInvariant();
            if (symptoms.Contains("chest pain") || symptoms.Contains("breathing difficulty") || symptoms.Contains("unconscious"))
                score += 20;
            else if (symptoms.Contains("bleeding") || symptoms.Contains("severe"))
                score += 12;
        }

        return Math.Min(score, 100);
    }

    private static (string LevelCode, decimal Confidence, string Reason) MapRiskToPriority(
        decimal riskScore,
        ClassifyPatientPriorityRequestDto input)
    {
        var reasons = new List<string>();

        if (input.OxygenSaturation is < 90) reasons.Add("Critical oxygen saturation");
        if (input.HeartRate is > 120 or < 50) reasons.Add("Abnormal heart rate");
        if (input.BloodPressureSystolic is >= 180 or <= 90) reasons.Add("Critical blood pressure");
        if (input.HasRecentHospitalization) reasons.Add("Recent hospitalization");
        if (input.SymptomSeverityScore is >= 8) reasons.Add("High symptom severity");

        if (reasons.Count == 0)
            reasons.Add("Composite clinical risk assessment");

        if (riskScore >= 70)
            return ("CRITICAL", 0.85m + (riskScore - 70) / 200, string.Join("; ", reasons));

        if (riskScore >= 45)
            return ("HIGH", 0.75m + (riskScore - 45) / 200, string.Join("; ", reasons));

        if (riskScore >= 25)
            return ("MEDIUM", 0.70m + (riskScore - 25) / 200, string.Join("; ", reasons));

        return ("LOW", 0.80m + (25 - riskScore) / 200, string.Join("; ", reasons));
    }
}
