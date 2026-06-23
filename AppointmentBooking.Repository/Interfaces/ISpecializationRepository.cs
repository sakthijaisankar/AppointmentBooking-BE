using AppointmentBooking.Database.Entities;

namespace AppointmentBooking.Repository.Interfaces;

public interface ISpecializationRepository
{
    Task<Specialization?> GetByIdAsync(int specializationId, CancellationToken cancellationToken = default);
    Task<Specialization?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Specialization>> GetAllActiveAsync(CancellationToken cancellationToken = default);
    Task<Specialization> CreateAsync(Specialization specialization, CancellationToken cancellationToken = default);
    Task UpdateAsync(Specialization specialization, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
