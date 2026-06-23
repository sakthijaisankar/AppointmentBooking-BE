using System.Text.Json;
using Microsoft.ML;
using Microsoft.ML.Data;
using AppointmentBooking.Application.DTOs.PatientPriority;
using AppointmentBooking.Application.Interfaces.ML;

namespace AppointmentBooking.Application.Services.ML;

public class PriorityPredictionInput
{
    public float Age { get; set; }
    public string Gender { get; set; } = string.Empty;
    public float HeartRate { get; set; }
    public float BloodPressureSystolic { get; set; }
    public float BloodPressureDiastolic { get; set; }
    public float TemperatureCelsius { get; set; }
    public float OxygenSaturation { get; set; }
    public float PainLevel { get; set; }
    public float SymptomSeverityScore { get; set; }
    public float HasChronicCondition { get; set; } // 1.0f = true, 0.0f = false
    public float HasRecentHospitalization { get; set; } // 1.0f = true, 0.0f = false
    public string PrimarySymptoms { get; set; } = string.Empty;
    public string Comorbidities { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class PriorityPredictionOutput
{
    [ColumnName("PredictedLabel")]
    public string PredictedLabel { get; set; } = string.Empty;
    public float[] Score { get; set; } = null!;
}

/// <summary>
/// ML.NET-based Patient Priority Classification Engine.
/// Trains an SDCA multiclass classifier on startup if the model zip does not exist.
/// Uses synchronized evaluation to ensure thread safety of the PredictionEngine.
/// </summary>
public class PatientPriorityPredictionEngine : IPatientPriorityPredictionEngine
{
    private readonly string _modelPath;
    private readonly MLContext _mlContext;
    private ITransformer? _model;
    private PredictionEngine<PriorityPredictionInput, PriorityPredictionOutput>? _predictionEngine;
    private readonly object _predictionLock = new();

    public PatientPriorityPredictionEngine()
    {
        _mlContext = new MLContext(seed: 42);
        _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ML", "Models", "patient-priority-v1.zip");

        InitializeEngine();
    }

    private void InitializeEngine()
    {
        try
        {
            var directory = Path.GetDirectoryName(_modelPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(_modelPath))
            {
                // Generate training data & train the model
                var syntheticData = GenerateSyntheticData();
                var trainingData = _mlContext.Data.LoadFromEnumerable(syntheticData);

                var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(outputColumnName: "LabelKey", inputColumnName: nameof(PriorityPredictionInput.Label))
                    .Append(_mlContext.Transforms.Text.FeaturizeText(outputColumnName: "SymptomsFeaturized", inputColumnName: nameof(PriorityPredictionInput.PrimarySymptoms)))
                    .Append(_mlContext.Transforms.Text.FeaturizeText(outputColumnName: "ComorbiditiesFeaturized", inputColumnName: nameof(PriorityPredictionInput.Comorbidities)))
                    .Append(_mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "GenderEncoded", inputColumnName: nameof(PriorityPredictionInput.Gender)))
                    .Append(_mlContext.Transforms.Concatenate("Features",
                        nameof(PriorityPredictionInput.Age),
                        nameof(PriorityPredictionInput.HeartRate),
                        nameof(PriorityPredictionInput.BloodPressureSystolic),
                        nameof(PriorityPredictionInput.BloodPressureDiastolic),
                        nameof(PriorityPredictionInput.TemperatureCelsius),
                        nameof(PriorityPredictionInput.OxygenSaturation),
                        nameof(PriorityPredictionInput.PainLevel),
                        nameof(PriorityPredictionInput.SymptomSeverityScore),
                        nameof(PriorityPredictionInput.HasChronicCondition),
                        nameof(PriorityPredictionInput.HasRecentHospitalization),
                        "SymptomsFeaturized",
                        "ComorbiditiesFeaturized",
                        "GenderEncoded"))
                    .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy(labelColumnName: "LabelKey", featureColumnName: "Features"))
                    .Append(_mlContext.Transforms.Conversion.MapKeyToValue(outputColumnName: "PredictedLabel", inputColumnName: "PredictedLabel"));

                _model = pipeline.Fit(trainingData);

                // Save the model
                _mlContext.Model.Save(_model, trainingData.Schema, _modelPath);
            }
            else
            {
                _model = _mlContext.Model.Load(_modelPath, out _);
            }

            _predictionEngine = _mlContext.Model.CreatePredictionEngine<PriorityPredictionInput, PriorityPredictionOutput>(_model);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing PatientPriorityPredictionEngine: {ex.Message}");
            throw;
        }
    }

    public PriorityPredictionResult Predict(ClassifyPatientPriorityRequestDto input)
    {
        var mlInput = new PriorityPredictionInput
        {
            Age = input.Age,
            Gender = input.Gender ?? "Unknown",
            HeartRate = input.HeartRate ?? 80.0f,
            BloodPressureSystolic = input.BloodPressureSystolic ?? 120.0f,
            BloodPressureDiastolic = input.BloodPressureDiastolic ?? 80.0f,
            TemperatureCelsius = (float)(input.TemperatureCelsius ?? 36.8m),
            OxygenSaturation = (float)(input.OxygenSaturation ?? 98.0m),
            PainLevel = input.PainLevel ?? 0,
            SymptomSeverityScore = input.SymptomSeverityScore ?? 0,
            HasChronicCondition = input.HasChronicCondition ? 1.0f : 0.0f,
            HasRecentHospitalization = input.HasRecentHospitalization ? 1.0f : 0.0f,
            PrimarySymptoms = input.PrimarySymptoms ?? string.Empty,
            Comorbidities = input.Comorbidities ?? string.Empty
        };

        PriorityPredictionOutput prediction;
        lock (_predictionLock)
        {
            if (_predictionEngine == null)
            {
                InitializeEngine();
            }
            prediction = _predictionEngine!.Predict(mlInput);
        }

        var riskScore = CalculateRiskScore(input);
        var reasons = GetClassificationReasons(input);
        var levelCode = MapPredictionToLevelCode(prediction.PredictedLabel, riskScore);
        var confidence = GetConfidenceScore(prediction);

        return new PriorityPredictionResult
        {
            PredictedLevelCode = levelCode,
            ConfidenceScore = Math.Round((decimal)confidence, 4),
            RiskScore = Math.Round(riskScore, 4),
            ClassificationReason = string.Join("; ", reasons),
            InputFeaturesJson = JsonSerializer.Serialize(input)
        };
    }

    private static float GetConfidenceScore(PriorityPredictionOutput output)
    {
        if (output.Score == null || output.Score.Length == 0)
            return 0.85f;

        return output.Score.Max();
    }

    private static string MapPredictionToLevelCode(string predictedLabel, decimal riskScore)
    {
        return predictedLabel switch
        {
            "Emergency" => "CRITICAL",
            "Priority" => riskScore >= 45 ? "HIGH" : "MEDIUM",
            "Normal" => "LOW",
            _ => "LOW"
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

    private static List<string> GetClassificationReasons(ClassifyPatientPriorityRequestDto input)
    {
        var reasons = new List<string>();

        if (input.OxygenSaturation is < 90) reasons.Add("Critical oxygen saturation (<90%)");
        else if (input.OxygenSaturation is < 94) reasons.Add("Sub-optimal oxygen saturation (<94%)");

        if (input.HeartRate is > 120 or < 50) reasons.Add("Severely abnormal heart rate");
        else if (input.HeartRate is > 100 or < 60) reasons.Add("Mildly abnormal heart rate");

        if (input.BloodPressureSystolic is >= 180 or <= 90) reasons.Add("Critical blood pressure");
        else if (input.BloodPressureSystolic is >= 140) reasons.Add("Elevated systolic blood pressure");

        if (input.TemperatureCelsius is >= 39.5m or <= 35.0m) reasons.Add("Critical body temperature");
        else if (input.TemperatureCelsius is >= 38.5m) reasons.Add("Elevated body temperature");

        if (input.PainLevel is >= 8) reasons.Add("Severe pain reported");
        if (input.SymptomSeverityScore is >= 8) reasons.Add("High symptom severity");

        if (input.HasRecentHospitalization) reasons.Add("Recent hospitalization history");
        if (input.HasChronicCondition) reasons.Add("Existing chronic condition");

        if (!string.IsNullOrWhiteSpace(input.PrimarySymptoms))
        {
            var symptoms = input.PrimarySymptoms.ToLowerInvariant();
            if (symptoms.Contains("chest pain") || symptoms.Contains("breathing difficulty") || symptoms.Contains("unconscious"))
                reasons.Add("High-risk symptoms detected");
        }

        if (reasons.Count == 0)
            reasons.Add("Routine clinical assessment");

        return reasons;
    }

    private static List<PriorityPredictionInput> GenerateSyntheticData()
    {
        var list = new List<PriorityPredictionInput>();
        var rand = new Random(42);

        string[] genders = { "Male", "Female", "Other", "Unknown" };

        // Generate balanced records: ~800 Normal, ~700 Priority, ~500 Emergency

        // Class 1: Normal (approx 800 records)
        for (int i = 0; i < 800; i++)
        {
            int age = rand.Next(5, 65);
            string gender = genders[rand.Next(genders.Length)];
            int heartRate = rand.Next(60, 95);
            int bpSystolic = rand.Next(100, 135);
            int bpDiastolic = rand.Next(65, 85);
            decimal temp = (decimal)(rand.NextDouble() * 1.4 + 36.1); // 36.1 to 37.5
            decimal spo2 = (decimal)(rand.NextDouble() * 5.0 + 95.0); // 95 to 100
            int pain = rand.Next(0, 4);
            int severity = rand.Next(0, 4);
            bool chronic = rand.Next(20) == 0;
            bool hosp = false;

            string symptoms = "";
            string comorbidities = "";
            if (rand.Next(4) == 0)
            {
                var symptList = new[] { "mild headache", "cough", "nausea", "fatigue" };
                symptoms = symptList[rand.Next(symptList.Length)];
            }

            list.Add(new PriorityPredictionInput
            {
                Age = age,
                Gender = gender,
                HeartRate = heartRate,
                BloodPressureSystolic = bpSystolic,
                BloodPressureDiastolic = bpDiastolic,
                TemperatureCelsius = (float)temp,
                OxygenSaturation = (float)spo2,
                PainLevel = pain,
                SymptomSeverityScore = severity,
                HasChronicCondition = chronic ? 1.0f : 0.0f,
                HasRecentHospitalization = hosp ? 1.0f : 0.0f,
                PrimarySymptoms = symptoms,
                Comorbidities = comorbidities,
                Label = "Normal"
            });
        }

        // Class 2: Priority (approx 700 records)
        for (int i = 0; i < 700; i++)
        {
            int age = rand.Next(0, 95);
            string gender = genders[rand.Next(genders.Length)];
            int heartRate = rand.Next(2) == 0 ? rand.Next(95, 120) : rand.Next(50, 60);
            int bpSystolic = rand.Next(2) == 0 ? rand.Next(135, 160) : rand.Next(80, 100);
            int bpDiastolic = rand.Next(2) == 0 ? rand.Next(85, 100) : rand.Next(50, 65);
            decimal temp = (decimal)(rand.NextDouble() * 1.5 + 37.5); // 37.5 to 39.0
            decimal spo2 = (decimal)(rand.NextDouble() * 4.0 + 90.0); // 90 to 94
            int pain = rand.Next(4, 8);
            int severity = rand.Next(4, 8);
            bool chronic = rand.Next(3) == 0; // 33% chance
            bool hosp = rand.Next(8) == 0;    // 12.5% chance

            string symptoms = "fever";
            string comorbidities = "";
            if (rand.Next(3) == 0)
            {
                var symptList = new[] { "bleeding", "severe headache", "nausea", "muscle pain" };
                symptoms = symptList[rand.Next(symptList.Length)];
            }
            if (rand.Next(3) == 0)
            {
                var comorbList = new[] { "diabetes", "asthma", "hypertension" };
                comorbidities = comorbList[rand.Next(comorbList.Length)];
            }

            list.Add(new PriorityPredictionInput
            {
                Age = age,
                Gender = gender,
                HeartRate = heartRate,
                BloodPressureSystolic = bpSystolic,
                BloodPressureDiastolic = bpDiastolic,
                TemperatureCelsius = (float)temp,
                OxygenSaturation = (float)spo2,
                PainLevel = pain,
                SymptomSeverityScore = severity,
                HasChronicCondition = chronic ? 1.0f : 0.0f,
                HasRecentHospitalization = hosp ? 1.0f : 0.0f,
                PrimarySymptoms = symptoms,
                Comorbidities = comorbidities,
                Label = "Priority"
            });
        }

        // Class 3: Emergency (approx 500 records)
        for (int i = 0; i < 500; i++)
        {
            int age = rand.Next(0, 100);
            string gender = genders[rand.Next(genders.Length)];
            int heartRate = rand.Next(2) == 0 ? rand.Next(120, 180) : rand.Next(30, 50);
            int bpSystolic = rand.Next(2) == 0 ? rand.Next(160, 220) : rand.Next(50, 80);
            int bpDiastolic = rand.Next(2) == 0 ? rand.Next(100, 130) : rand.Next(30, 50);
            decimal temp = rand.Next(2) == 0 ? (decimal)(rand.NextDouble() * 3.0 + 39.0) : (decimal)(rand.NextDouble() * 2.0 + 33.0); // >39.0 or <35.0
            decimal spo2 = (decimal)(rand.NextDouble() * 20.0 + 65.0); // 65 to 85 (Critical < 90)
            int pain = rand.Next(8, 11);
            int severity = rand.Next(8, 11);
            bool chronic = rand.Next(2) == 0;
            bool hosp = rand.Next(2) == 0;

            string symptoms = "chest pain";
            string comorbidities = "heart disease";
            if (rand.Next(3) == 0)
            {
                var symptList = new[] { "breathing difficulty", "unconscious", "severe bleeding" };
                symptoms = symptList[rand.Next(symptList.Length)];
            }
            if (rand.Next(3) == 0)
            {
                var comorbList = new[] { "copd", "kidney disease", "congestive heart failure" };
                comorbidities = comorbList[rand.Next(comorbList.Length)];
            }

            list.Add(new PriorityPredictionInput
            {
                Age = age,
                Gender = gender,
                HeartRate = heartRate,
                BloodPressureSystolic = bpSystolic,
                BloodPressureDiastolic = bpDiastolic,
                TemperatureCelsius = (float)temp,
                OxygenSaturation = (float)spo2,
                PainLevel = pain,
                SymptomSeverityScore = severity,
                HasChronicCondition = chronic ? 1.0f : 0.0f,
                HasRecentHospitalization = hosp ? 1.0f : 0.0f,
                PrimarySymptoms = symptoms,
                Comorbidities = comorbidities,
                Label = "Emergency"
            });
        }

        return list;
    }
}
