using AppointmentBooking.Application.DTOs.PatientPriority;

namespace AppointmentBooking.Application.Interfaces.ML;

public interface IPatientPriorityPredictionEngine
{
    PriorityPredictionResult Predict(ClassifyPatientPriorityRequestDto input);
}
