using AppointmentBooking.Database;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Repository.Implementations;

public class PatientPriorityRepository : IPatientPriorityRepository
{
    private readonly AppointmentBookingDbContext _context;

    public PatientPriorityRepository(AppointmentBookingDbContext context)
    {
        _context = context;
    }

    public Task<Patient?> GetPatientByIdAsync(int patientId, CancellationToken cancellationToken = default) =>
        _context.Patients.FirstOrDefaultAsync(p => p.PatientId == patientId && p.IsActive, cancellationToken);

    public Task<Appointment?> GetAppointmentByIdAsync(int appointmentId, CancellationToken cancellationToken = default) =>
        _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId, cancellationToken);

    public async Task<IReadOnlyList<PriorityLevel>> GetActivePriorityLevelsAsync(CancellationToken cancellationToken = default) =>
        await _context.PriorityLevels
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .ToListAsync(cancellationToken);

    public Task<PriorityLevel?> GetPriorityLevelByIdAsync(int priorityLevelId, CancellationToken cancellationToken = default) =>
        _context.PriorityLevels.FirstOrDefaultAsync(p => p.PriorityLevelId == priorityLevelId && p.IsActive, cancellationToken);

    public Task<PriorityLevel?> GetPriorityLevelByCodeAsync(string levelCode, CancellationToken cancellationToken = default) =>
        _context.PriorityLevels.FirstOrDefaultAsync(p => p.LevelCode == levelCode && p.IsActive, cancellationToken);

    public Task<MlModelVersion?> GetActiveModelVersionAsync(CancellationToken cancellationToken = default) =>
        _context.MlModelVersions
            .Where(m => m.IsActive)
            .OrderByDescending(m => m.DeployedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<PatientPriorityClassification?> GetCurrentClassificationAsync(int patientId, CancellationToken cancellationToken = default) =>
        _context.PatientPriorityClassifications
            .Include(c => c.PredictedPriorityLevel)
            .Include(c => c.MlModelVersion)
            .Include(c => c.PatientClinicalFeature)
            .Include(c => c.Overrides).ThenInclude(o => o.OverridePriorityLevel)
            .Include(c => c.Patient)
            .FirstOrDefaultAsync(c => c.PatientId == patientId && c.IsCurrent, cancellationToken);

    public Task<PatientPriorityClassification?> GetClassificationByIdAsync(int classificationId, CancellationToken cancellationToken = default) =>
        _context.PatientPriorityClassifications
            .Include(c => c.PredictedPriorityLevel)
            .Include(c => c.MlModelVersion)
            .Include(c => c.PatientClinicalFeature)
            .Include(c => c.Overrides).ThenInclude(o => o.OverridePriorityLevel)
            .Include(c => c.Patient)
            .FirstOrDefaultAsync(c => c.PatientPriorityClassificationId == classificationId, cancellationToken);

    public async Task<IReadOnlyList<PatientPriorityClassification>> GetClassificationHistoryAsync(
        int patientId, int pageNumber, int pageSize, CancellationToken cancellationToken = default) =>
        await _context.PatientPriorityClassifications
            .Include(c => c.PredictedPriorityLevel)
            .Include(c => c.MlModelVersion)
            .Include(c => c.PatientClinicalFeature)
            .Include(c => c.Overrides).ThenInclude(o => o.OverridePriorityLevel)
            .Include(c => c.Patient)
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.ClassifiedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<int> GetClassificationHistoryCountAsync(int patientId, CancellationToken cancellationToken = default) =>
        _context.PatientPriorityClassifications.CountAsync(c => c.PatientId == patientId, cancellationToken);

    public async Task<PatientClinicalFeature> AddClinicalFeaturesAsync(PatientClinicalFeature features, CancellationToken cancellationToken = default)
    {
        await _context.PatientClinicalFeatures.AddAsync(features, cancellationToken);
        return features;
    }

    public async Task<PatientPriorityClassification> AddClassificationAsync(PatientPriorityClassification classification, CancellationToken cancellationToken = default)
    {
        await _context.PatientPriorityClassifications.AddAsync(classification, cancellationToken);
        return classification;
    }

    public async Task<PriorityClassificationOverride> AddOverrideAsync(PriorityClassificationOverride overrideRecord, CancellationToken cancellationToken = default)
    {
        await _context.PriorityClassificationOverrides.AddAsync(overrideRecord, cancellationToken);
        return overrideRecord;
    }

    public async Task SetCurrentClassificationAsync(int patientId, int newClassificationId, CancellationToken cancellationToken = default)
    {
        var existing = await _context.PatientPriorityClassifications
            .Where(c => c.PatientId == patientId && c.IsCurrent && c.PatientPriorityClassificationId != newClassificationId)
            .ToListAsync(cancellationToken);

        foreach (var item in existing)
            item.IsCurrent = false;

        var current = await _context.PatientPriorityClassifications
            .FirstOrDefaultAsync(c => c.PatientPriorityClassificationId == newClassificationId, cancellationToken);

        if (current is not null)
            current.IsCurrent = true;
    }

    public async Task LinkAppointmentToClassificationAsync(int appointmentId, int classificationId, CancellationToken cancellationToken = default)
    {
        var appointment = await _context.Appointments.FirstOrDefaultAsync(a => a.AppointmentId == appointmentId, cancellationToken);
        if (appointment is not null)
            appointment.CurrentPriorityClassificationId = classificationId;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
