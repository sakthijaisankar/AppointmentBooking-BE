using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.DTOs.Doctors;

namespace AppointmentBooking.Application.Interfaces;

public interface IDoctorService
{
    // Doctor Profile Management
    Task<DoctorDetailDto> CreateProfileAsync(CreateDoctorProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<DoctorDetailDto> UpdateProfileAsync(int doctorId, UpdateDoctorProfileRequestDto request, CancellationToken cancellationToken = default);
    Task<DoctorDetailDto> GetByIdAsync(int doctorId, CancellationToken cancellationToken = default);
    Task<DoctorDetailDto?> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<PagedResult<DoctorProfileDto>> GetAllAsync(string? search, int? specializationId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task DeleteProfileAsync(int doctorId, CancellationToken cancellationToken = default);

    // Doctor Schedule Management
    Task<DoctorScheduleDto> AddScheduleAsync(int doctorId, CreateDoctorScheduleRequestDto request, CancellationToken cancellationToken = default);
    Task<DoctorScheduleDto> UpdateScheduleAsync(int scheduleId, UpdateDoctorScheduleRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DoctorScheduleDto>> GetSchedulesAsync(int doctorId, CancellationToken cancellationToken = default);
    Task DeleteScheduleAsync(int scheduleId, CancellationToken cancellationToken = default);
    Task<int> GetDoctorIdByScheduleIdAsync(int scheduleId, CancellationToken cancellationToken = default);

    // Specializations Master Management
    Task<SpecializationDto> CreateSpecializationAsync(CreateSpecializationRequestDto request, CancellationToken cancellationToken = default);
    Task<SpecializationDto> UpdateSpecializationAsync(int specializationId, UpdateSpecializationRequestDto request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SpecializationDto>> GetSpecializationsAsync(CancellationToken cancellationToken = default);
    Task DeleteSpecializationAsync(int specializationId, CancellationToken cancellationToken = default);
}
