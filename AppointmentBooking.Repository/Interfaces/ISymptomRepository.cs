using AppointmentBooking.Database.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBooking.Repository.Interfaces;

public interface ISymptomRepository
{
    Task<IReadOnlyList<Symptom>> GetActiveSymptomsAsync(CancellationToken cancellationToken = default);
    Task<Symptom?> GetByIdAsync(int symptomId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientSymptom>> GetSymptomsByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task AddPatientSymptomsAsync(IEnumerable<PatientSymptom> symptoms, CancellationToken cancellationToken = default);
    Task RemovePatientSymptomsAsync(IEnumerable<PatientSymptom> symptoms, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
