using AppointmentBooking.Application.Common;
using AppointmentBooking.Application.Constants;
using AppointmentBooking.Application.DTOs.Symptom;
using AppointmentBooking.Application.Exceptions;
using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Database.Entities;
using AppointmentBooking.Repository.Interfaces;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AppValidationException = AppointmentBooking.Application.Exceptions.ValidationException;

namespace AppointmentBooking.Application.Services;

public class SymptomService : ISymptomService
{
    private readonly ISymptomRepository _symptomRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IUserRepository _userRepository;
    private readonly IValidator<SubmitSymptomsRequestDto> _submitSymptomsValidator;

    public SymptomService(
        ISymptomRepository symptomRepository,
        IAppointmentRepository appointmentRepository,
        IPatientRepository patientRepository,
        IUserRepository userRepository,
        IValidator<SubmitSymptomsRequestDto> submitSymptomsValidator)
    {
        _symptomRepository = symptomRepository;
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _userRepository = userRepository;
        _submitSymptomsValidator = submitSymptomsValidator;
    }

    public async Task<IReadOnlyList<SymptomDto>> GetActiveSymptomsAsync(CancellationToken cancellationToken = default)
    {
        var symptoms = await _symptomRepository.GetActiveSymptomsAsync(cancellationToken);
        return symptoms.Select(s => new SymptomDto
        {
            SymptomId = s.SymptomId,
            SymptomName = s.SymptomName,
            Description = s.Description
        }).ToList();
    }

    public async Task<AppointmentSymptomsDetailDto?> GetSymptomsByAppointmentIdAsync(
        int appointmentId, 
        int userId, 
        CancellationToken cancellationToken = default)
    {
        var appointment = await _appointmentRepository.GetDetailByIdAsync(appointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        // Verify permissions (Admin, Receptionist, Doctor, or the Patient owner)
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var isStaff = user.UserRoles.Any(ur => AppRoles.Staff.Contains(ur.Role.RoleName));
        if (!isStaff)
        {
            var patient = await _patientRepository.GetByUserIdAsync(userId, cancellationToken);
            if (patient == null || appointment.PatientId != patient.PatientId)
            {
                throw new UnauthorizedException("Access denied. You cannot view symptoms for this appointment.");
            }
        }

        var submitted = await _symptomRepository.GetSymptomsByAppointmentIdAsync(appointmentId, cancellationToken);
        if (submitted.Count == 0)
        {
            return null;
        }

        var patientName = appointment.Patient != null ? $"{appointment.Patient.FirstName} {appointment.Patient.LastName}" : "Unknown";

        return new AppointmentSymptomsDetailDto
        {
            AppointmentId = appointmentId,
            AppointmentNumber = appointment.AppointmentNumber,
            PatientId = appointment.PatientId,
            PatientName = patientName,
            ExistingConditions = submitted.FirstOrDefault()?.ExistingConditions,
            SubmissionNotes = appointment.Notes,
            Symptoms = submitted.Select(s => new PatientSymptomDetailDto
            {
                PatientSymptomId = s.PatientSymptomId,
                SymptomId = s.SymptomId,
                SymptomName = s.Symptom?.SymptomName ?? "Unknown",
                SeverityLevel = s.SeverityLevel,
                Notes = s.Notes
            }).ToList()
        };
    }

    public async Task SubmitSymptomsAsync(
        SubmitSymptomsRequestDto request, 
        int userId, 
        CancellationToken cancellationToken = default)
    {
        await ValidateAsync(_submitSymptomsValidator, request, cancellationToken);

        var appointment = await _appointmentRepository.GetByIdAsync(request.AppointmentId, cancellationToken)
            ?? throw new NotFoundException("Appointment not found.");

        var status = await _appointmentRepository.GetStatusByIdAsync(appointment.AppointmentStatusId, cancellationToken)
            ?? throw new NotFoundException("Appointment status not found.");

        // Lock submission if not in Pending or Confirmed state
        if (status.StatusName != "Pending" && status.StatusName != "Confirmed")
        {
            throw new ConflictException($"Symptom submission is locked for appointments in the {status.StatusName} state.");
        }

        // Verify permissions (Staff can submit on behalf of patient, patient can submit for self)
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new NotFoundException("User not found.");

        var isStaff = user.UserRoles.Any(ur => AppRoles.Staff.Contains(ur.Role.RoleName));
        if (!isStaff)
        {
            var patient = await _patientRepository.GetByUserIdAsync(userId, cancellationToken);
            if (patient == null || appointment.PatientId != patient.PatientId)
            {
                throw new UnauthorizedException("You are not authorized to submit symptoms for this appointment.");
            }
        }

        // Transactional overwrite of existing symptoms for this appointment
        var existing = await _symptomRepository.GetSymptomsByAppointmentIdAsync(request.AppointmentId, cancellationToken);
        if (existing.Count > 0)
        {
            await _symptomRepository.RemovePatientSymptomsAsync(existing, cancellationToken);
        }

        // Insert new symptoms
        var patientSymptoms = new List<PatientSymptom>();
        foreach (var sym in request.Symptoms)
        {
            var symptomMaster = await _symptomRepository.GetByIdAsync(sym.SymptomId, cancellationToken)
                ?? throw new NotFoundException($"Symptom ID {sym.SymptomId} not found.");

            if (!symptomMaster.IsActive)
            {
                throw new ConflictException($"Symptom '{symptomMaster.SymptomName}' is currently inactive.");
            }

            patientSymptoms.Add(new PatientSymptom
            {
                AppointmentId = request.AppointmentId,
                SymptomId = sym.SymptomId,
                SeverityLevel = sym.SeverityLevel,
                ExistingConditions = request.ExistingConditions,
                Notes = sym.Notes,
                CreatedAt = DateTime.UtcNow
            });
        }

        // Copy the overall submission notes to the appointment's general Notes column for visibility
        appointment.Notes = request.Notes;
        appointment.UpdatedAt = DateTime.UtcNow;

        await _symptomRepository.AddPatientSymptomsAsync(patientSymptoms, cancellationToken);
        await _appointmentRepository.UpdateAsync(appointment, cancellationToken);

        await _symptomRepository.SaveChangesAsync(cancellationToken);
    }

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken cancellationToken)
    {
        var result = await validator.ValidateAsync(instance, cancellationToken);
        if (!result.IsValid)
        {
            throw new AppValidationException("Validation failed.", result.Errors.Select(e => e.ErrorMessage));
        }
    }
}
