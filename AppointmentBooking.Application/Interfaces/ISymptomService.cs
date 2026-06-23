using AppointmentBooking.Application.DTOs.Symptom;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBooking.Application.Interfaces;

public interface ISymptomService
{
    Task<IReadOnlyList<SymptomDto>> GetActiveSymptomsAsync(CancellationToken cancellationToken = default);
    Task<AppointmentSymptomsDetailDto?> GetSymptomsByAppointmentIdAsync(int appointmentId, int userId, CancellationToken cancellationToken = default);
    Task SubmitSymptomsAsync(SubmitSymptomsRequestDto request, int userId, CancellationToken cancellationToken = default);
}
