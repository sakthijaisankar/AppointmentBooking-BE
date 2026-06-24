using AppointmentBooking.Application.DTOs.Queue;
using AppointmentBooking.Application.Exceptions;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AppointmentBooking.Application.Services;

public class QueueService : IQueueService
{
    private readonly IQueueRepository _queueRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientPriorityRepository _patientPriorityRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly INotificationService _notificationService;

    public QueueService(
        IQueueRepository queueRepository,
        IAppointmentRepository appointmentRepository,
        IPatientPriorityRepository patientPriorityRepository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        INotificationService notificationService)
    {
        _queueRepository = queueRepository;
        _appointmentRepository = appointmentRepository;
        _patientPriorityRepository = patientPriorityRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _notificationService = notificationService;
    }

    public async Task<QueueEntryDto> CheckInPatientAsync(CheckInRequestDto request, CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException($"Appointment with ID {request.AppointmentId} was not found.");

        // Check if appointment date is today
        var today = DateTime.UtcNow.Date;
        if (appointment.ScheduledDateTime.Date != today && appointment.ScheduledDateTime.Date != DateTime.Today)
            throw new ValidationException("Can only check-in for appointments scheduled for today.");

        // Check if already checked in
        var existingQueue = await _queueRepository.GetQueueByAppointmentIdAsync(request.AppointmentId, cancellationToken);
        if (existingQueue != null)
            throw new ConflictException("Patient is already checked in for this appointment.");

        // Get patient priority classification
        var priorityClassification = await _patientPriorityRepository.GetCurrentClassificationAsync(appointment.PatientId, cancellationToken)
            ?? throw new ValidationException("Patient has not been classified for priority. Run triage classification first.");

        // Determine if priority classification is critical
        var latestOverride = priorityClassification.Overrides.OrderByDescending(o => o.OverriddenAt).FirstOrDefault();
        string effectiveCode = latestOverride != null 
            ? latestOverride.OverridePriorityLevel.LevelCode 
            : priorityClassification.PredictedPriorityLevel.LevelCode;

        bool isCritical = effectiveCode == "CRITICAL";

        // Generate daily queue number
        int dailyCount = await _queueRepository.GetDailyQueueCountAsync(DateTime.UtcNow, isCritical, cancellationToken);
        string prefix = isCritical ? "CRIT" : "Q";
        string queueNumber = $"{prefix}-{(dailyCount + 1):D3}";

        // Get default WAITING status
        var waitStatus = await _queueRepository.GetQueueStatusByCodeAsync("WAITING", cancellationToken)
            ?? throw new ConflictException("Default Queue status 'WAITING' is not configured in database.");

        var entry = new QueueManagement
        {
            AppointmentId = request.AppointmentId,
            PatientPriorityClassificationId = priorityClassification.PatientPriorityClassificationId,
            QueueNumber = queueNumber,
            QueueStatusId = waitStatus.QueueStatusId,
            CheckInTime = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _queueRepository.AddQueueEntryAsync(entry, cancellationToken);
        await _queueRepository.SaveChangesAsync(cancellationToken);

        // Recalculate wait times for all active patients of the doctor
        await RecalculateWaitTimesAsync(appointment.DoctorId, cancellationToken);
        await _queueRepository.SaveChangesAsync(cancellationToken);

        // Reload the queue item to return it fully loaded
        var reloaded = await _queueRepository.GetQueueByIdAsync(entry.QueueId, cancellationToken)
            ?? throw new NotFoundException($"Failed to reload check-in entry for QueueId {entry.QueueId}.");

        return MapToDto(reloaded);
    }

    public async Task<IReadOnlyList<QueueEntryDto>> GetActiveQueueAsync(int? doctorId, CancellationToken cancellationToken = default)
    {
        var list = await _queueRepository.GetActiveQueueAsync(doctorId, cancellationToken);
        return list.Select(MapToDto).ToList();
    }

    public async Task<QueueEntryDto> UpdateQueueStatusAsync(int queueId, UpdateQueueStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        var entry = await _queueRepository.GetQueueByIdAsync(queueId, cancellationToken)
            ?? throw new NotFoundException($"Queue entry with ID {queueId} was not found.");

        var targetStatus = await _queueRepository.GetQueueStatusByCodeAsync(request.StatusCode.ToUpperInvariant(), cancellationToken)
            ?? throw new NotFoundException($"Queue status with code '{request.StatusCode}' was not found.");

        entry.QueueStatusId = targetStatus.QueueStatusId;
        entry.QueueStatus = targetStatus;
        entry.UpdatedAt = DateTime.UtcNow;

        // Perform time stamp captures and cross-module appointment status updates
        switch (request.StatusCode.ToUpperInvariant())
        {
            case "CALLING":
                entry.CallingTime = DateTime.UtcNow;
                try
                {
                    var appointment = await _appointmentRepository.GetByIdAsync(entry.AppointmentId, cancellationToken);
                    if (appointment != null)
                    {
                        var pat = await _patientRepository.GetByIdAsync(appointment.PatientId, cancellationToken);
                        var doc = await _doctorRepository.GetByIdAsync(appointment.DoctorId, cancellationToken);
                        if (pat != null && doc != null)
                        {
                            await _notificationService.SendNotificationAsync(
                                "QUEUE_CALLING",
                                pat.UserId,
                                appointment.AppointmentId,
                                new Dictionary<string, string>
                                {
                                    { "PatientName", $"{pat.FirstName} {pat.LastName}" },
                                    { "DoctorName", $"{doc.FirstName} {doc.LastName}" },
                                    { "QueueNumber", entry.QueueNumber }
                                },
                                cancellationToken
                            );
                        }
                    }
                }
                catch
                {
                    // Fail-safe
                }
                break;

            case "IN_CONSULTATION":
                entry.ConsultationStartTime = DateTime.UtcNow;
                // Transition appointment status to InProgress
                var inProgressStatus = await _appointmentRepository.GetStatusByNameAsync("InProgress", cancellationToken);
                if (inProgressStatus != null)
                {
                    entry.Appointment.AppointmentStatusId = inProgressStatus.AppointmentStatusId;
                    await _appointmentRepository.UpdateAsync(entry.Appointment, cancellationToken);
                }
                break;

            case "COMPLETED":
                entry.ConsultationEndTime = DateTime.UtcNow;
                // Transition appointment status to Completed
                var completedStatus = await _appointmentRepository.GetStatusByNameAsync("Completed", cancellationToken);
                if (completedStatus != null)
                {
                    entry.Appointment.AppointmentStatusId = completedStatus.AppointmentStatusId;
                    await _appointmentRepository.UpdateAsync(entry.Appointment, cancellationToken);
                }
                break;

            case "SKIPPED":
                entry.ConsultationEndTime = DateTime.UtcNow;
                // Transition appointment status to NoShow
                var noShowStatus = await _appointmentRepository.GetStatusByNameAsync("NoShow", cancellationToken);
                if (noShowStatus != null)
                {
                    entry.Appointment.AppointmentStatusId = noShowStatus.AppointmentStatusId;
                    await _appointmentRepository.UpdateAsync(entry.Appointment, cancellationToken);
                }
                break;
        }

        await _queueRepository.SaveChangesAsync(cancellationToken);

        // Recalculate wait times for all active patients of the doctor
        await RecalculateWaitTimesAsync(entry.Appointment.DoctorId, cancellationToken);
        await _queueRepository.SaveChangesAsync(cancellationToken);

        // Reload the queue item
        var reloaded = await _queueRepository.GetQueueByIdAsync(queueId, cancellationToken)
            ?? throw new NotFoundException($"Failed to reload queue item.");

        return MapToDto(reloaded);
    }

    public async Task<PatientQueueStatusDto> GetPatientQueueStatusAsync(int patientId, CancellationToken cancellationToken = default)
    {
        // Get all active queue entries across all doctors
        var activeQueue = await _queueRepository.GetActiveQueueAsync(null, cancellationToken);

        // Find patient's active entry
        var patientEntry = activeQueue.FirstOrDefault(q => q.Appointment.PatientId == patientId);
        if (patientEntry == null)
        {
            return new PatientQueueStatusDto
            {
                IsCheckedIn = false,
                PositionAhead = 0,
                EstimatedWaitTimeMinutes = 0
            };
        }

        // Filter active queue by their doctor to compute position ahead
        var doctorActiveQueue = activeQueue.Where(q => q.Appointment.DoctorId == patientEntry.Appointment.DoctorId).ToList();
        int patientIndex = doctorActiveQueue.IndexOf(patientEntry);

        int positionAhead = 0;
        for (int i = 0; i < patientIndex; i++)
        {
            var code = doctorActiveQueue[i].QueueStatus.StatusCode;
            if (code == "WAITING" || code == "CALLING")
            {
                positionAhead++;
            }
        }

        return new PatientQueueStatusDto
        {
            IsCheckedIn = true,
            QueueNumber = patientEntry.QueueNumber,
            StatusCode = patientEntry.QueueStatus.StatusCode,
            StatusName = patientEntry.QueueStatus.StatusName,
            PositionAhead = positionAhead,
            EstimatedWaitTimeMinutes = patientEntry.EstimatedWaitTimeMinutes,
            DoctorName = $"Dr. {patientEntry.Appointment.Doctor.FirstName} {patientEntry.Appointment.Doctor.LastName}"
        };
    }

    public async Task<IReadOnlyList<QueueStatusDto>> GetQueueStatusesAsync(CancellationToken cancellationToken = default)
    {
        var list = await _queueRepository.GetQueueStatusesAsync(cancellationToken);
        return list.Select(s => new QueueStatusDto
        {
            QueueStatusId = s.QueueStatusId,
            StatusCode = s.StatusCode,
            StatusName = s.StatusName,
            Description = s.Description
        }).ToList();
    }

    private async Task RecalculateWaitTimesAsync(int doctorId, CancellationToken cancellationToken)
    {
        var activeQueue = await _queueRepository.GetActiveQueueAsync(doctorId, cancellationToken);

        int accumulatedWait = 0;

        foreach (var q in activeQueue)
        {
            var code = q.QueueStatus.StatusCode;
            if (code == "IN_CONSULTATION")
            {
                q.EstimatedWaitTimeMinutes = 0;
                accumulatedWait += 10; // Assume 10 mins remaining for active consultation
            }
            else if (code == "CALLING")
            {
                q.EstimatedWaitTimeMinutes = 0;
                accumulatedWait += 5; // Assume 5 mins for patient to walk in
            }
            else if (code == "WAITING")
            {
                q.EstimatedWaitTimeMinutes = accumulatedWait;
                accumulatedWait += GetExpectedServiceTime(q.PatientPriorityClassification);
            }
        }
    }

    private static int GetExpectedServiceTime(PatientPriorityClassification classification)
    {
        var latestOverride = classification.Overrides.OrderByDescending(o => o.OverriddenAt).FirstOrDefault();
        string code = latestOverride != null 
            ? latestOverride.OverridePriorityLevel.LevelCode 
            : classification.PredictedPriorityLevel.LevelCode;

        return code switch
        {
            "CRITICAL" => 45,
            "HIGH" => 20,
            "MEDIUM" => 20,
            "LOW" => 15,
            _ => 15
        };
    }

    private static QueueEntryDto MapToDto(QueueManagement q)
    {
        var classification = q.PatientPriorityClassification;
        var latestOverride = classification.Overrides.OrderByDescending(o => o.OverriddenAt).FirstOrDefault();
        
        var effectiveLevel = latestOverride != null 
            ? latestOverride.OverridePriorityLevel 
            : classification.PredictedPriorityLevel;

        return new QueueEntryDto
        {
            QueueId = q.QueueId,
            AppointmentId = q.AppointmentId,
            AppointmentNumber = q.Appointment.AppointmentNumber,
            PatientId = q.Appointment.PatientId,
            PatientCode = q.Appointment.Patient.PatientCode,
            PatientName = $"{q.Appointment.Patient.FirstName} {q.Appointment.Patient.LastName}",
            DoctorId = q.Appointment.DoctorId,
            DoctorName = $"Dr. {q.Appointment.Doctor.FirstName} {q.Appointment.Doctor.LastName}",
            QueueNumber = q.QueueNumber,
            QueueStatus = new QueueStatusDto
            {
                QueueStatusId = q.QueueStatus.QueueStatusId,
                StatusCode = q.QueueStatus.StatusCode,
                StatusName = q.QueueStatus.StatusName,
                Description = q.QueueStatus.Description
            },
            PriorityLevelCode = effectiveLevel.LevelCode,
            PriorityLevelName = effectiveLevel.LevelName,
            PriorityColorHex = effectiveLevel.ColorHex,
            EstimatedWaitTimeMinutes = q.EstimatedWaitTimeMinutes,
            CheckInTime = q.CheckInTime,
            CallingTime = q.CallingTime,
            ConsultationStartTime = q.ConsultationStartTime,
            ConsultationEndTime = q.ConsultationEndTime
        };
    }
}
