using AppointmentBooking.Database.Entities;

namespace AppointmentBooking.Repository.Interfaces;

public interface IConsultationRepository
{
    Task<Consultation?> GetByIdAsync(int consultationId, CancellationToken cancellationToken = default);
    Task<Consultation?> GetByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Consultation> Items, int TotalCount)> GetByPatientIdAsync(int patientId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Consultation> Items, int TotalCount)> GetByDoctorIdAsync(int doctorId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<Prescription?> GetPrescriptionByIdAsync(int prescriptionId, CancellationToken cancellationToken = default);
    Task AddAsync(Consultation consultation, CancellationToken cancellationToken = default);
    Task AddPrescriptionAsync(Prescription prescription, CancellationToken cancellationToken = default);
    Task RemovePrescriptionAsync(Prescription prescription, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
