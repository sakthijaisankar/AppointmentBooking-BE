using AppointmentBooking.Database;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Repository.Implementations;

public class QueueRepository : IQueueRepository
{
    private readonly AppointmentBookingDbContext _context;

    public QueueRepository(AppointmentBookingDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<QueueManagement>> GetActiveQueueAsync(int? doctorId, CancellationToken cancellationToken = default)
    {
        var query = _context.QueueManagements
            .Include(q => q.QueueStatus)
            .Include(q => q.PatientPriorityClassification)
                .ThenInclude(c => c.PredictedPriorityLevel)
            .Include(q => q.PatientPriorityClassification)
                .ThenInclude(c => c.Overrides)
                    .ThenInclude(o => o.OverridePriorityLevel)
            .Include(q => q.Appointment)
                .ThenInclude(a => a.Patient)
            .Include(q => q.Appointment)
                .ThenInclude(a => a.Doctor)
            .Where(q => q.QueueStatus.StatusCode == "WAITING" || 
                        q.QueueStatus.StatusCode == "CALLING" || 
                        q.QueueStatus.StatusCode == "IN_CONSULTATION");

        if (doctorId.HasValue)
        {
            query = query.Where(q => q.Appointment.DoctorId == doctorId.Value);
        }

        var list = await query.ToListAsync(cancellationToken);

        // Sort in-memory to handle manual overrides accurately
        return list
            .OrderBy(q => GetEffectiveSortOrder(q.PatientPriorityClassification))
            .ThenBy(q => q.CheckInTime)
            .ToList();
    }

    private static int GetEffectiveSortOrder(PatientPriorityClassification classification)
    {
        var latestOverride = classification.Overrides
            .OrderByDescending(o => o.OverriddenAt)
            .FirstOrDefault();

        if (latestOverride != null)
        {
            return latestOverride.OverridePriorityLevel.SortOrder;
        }

        return classification.PredictedPriorityLevel.SortOrder;
    }

    public Task<QueueManagement?> GetQueueByIdAsync(int queueId, CancellationToken cancellationToken = default) =>
        _context.QueueManagements
            .Include(q => q.QueueStatus)
            .Include(q => q.PatientPriorityClassification)
                .ThenInclude(c => c.PredictedPriorityLevel)
            .Include(q => q.PatientPriorityClassification)
                .ThenInclude(c => c.Overrides)
                    .ThenInclude(o => o.OverridePriorityLevel)
            .Include(q => q.Appointment)
                .ThenInclude(a => a.Patient)
            .Include(q => q.Appointment)
                .ThenInclude(a => a.Doctor)
            .FirstOrDefaultAsync(q => q.QueueId == queueId, cancellationToken);

    public Task<QueueManagement?> GetQueueByAppointmentIdAsync(int appointmentId, CancellationToken cancellationToken = default) =>
        _context.QueueManagements
            .Include(q => q.QueueStatus)
            .Include(q => q.PatientPriorityClassification)
                .ThenInclude(c => c.PredictedPriorityLevel)
            .Include(q => q.PatientPriorityClassification)
                .ThenInclude(c => c.Overrides)
                    .ThenInclude(o => o.OverridePriorityLevel)
            .Include(q => q.Appointment)
                .ThenInclude(a => a.Patient)
            .FirstOrDefaultAsync(q => q.AppointmentId == appointmentId, cancellationToken);

    public Task<QueueStatus?> GetQueueStatusByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _context.QueueStatuses.FirstOrDefaultAsync(s => s.StatusCode == code && s.IsActive, cancellationToken);

    public async Task<IReadOnlyList<QueueStatus>> GetQueueStatusesAsync(CancellationToken cancellationToken = default) =>
        await _context.QueueStatuses.Where(s => s.IsActive).ToListAsync(cancellationToken);

    public async Task<QueueManagement> AddQueueEntryAsync(QueueManagement entry, CancellationToken cancellationToken = default)
    {
        await _context.QueueManagements.AddAsync(entry, cancellationToken);
        return entry;
    }

    public Task<int> GetDailyQueueCountAsync(DateTime date, bool isCritical, CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        var query = _context.QueueManagements
            .Where(q => q.CheckInTime >= startOfDay && q.CheckInTime < endOfDay);

        if (isCritical)
        {
            query = query.Where(q => q.PatientPriorityClassification.PredictedPriorityLevel.LevelCode == "CRITICAL" || 
                                     q.PatientPriorityClassification.Overrides
                                         .OrderByDescending(o => o.OverriddenAt)
                                         .FirstOrDefault()!.OverridePriorityLevel.LevelCode == "CRITICAL");
        }
        else
        {
            query = query.Where(q => q.PatientPriorityClassification.PredictedPriorityLevel.LevelCode != "CRITICAL" && 
                                     (q.PatientPriorityClassification.Overrides
                                         .OrderByDescending(o => o.OverriddenAt)
                                         .FirstOrDefault() == null ||
                                      q.PatientPriorityClassification.Overrides
                                         .OrderByDescending(o => o.OverriddenAt)
                                         .FirstOrDefault()!.OverridePriorityLevel.LevelCode != "CRITICAL"));
        }

        return query.CountAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _context.SaveChangesAsync(cancellationToken);
}
