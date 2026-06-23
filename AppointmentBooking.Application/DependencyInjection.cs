using AppointmentBooking.Application.Interfaces;
using AppointmentBooking.Application.Interfaces.ML;
using AppointmentBooking.Application.Services;
using AppointmentBooking.Application.Services.ML;
using AppointmentBooking.Application.Validators;
using AppointmentBooking.Application.DTOs.PatientPriority;
using AppointmentBooking.Application.DTOs.Patients;
using AppointmentBooking.Application.DTOs.Auth;
using AppointmentBooking.Application.DTOs.Doctors;
using AppointmentBooking.Application.DTOs.Appointment;
using AppointmentBooking.Application.DTOs.Symptom;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AppointmentBooking.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IPatientPriorityService, PatientPriorityService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IDoctorService, DoctorService>();
        services.AddScoped<IAppointmentService, AppointmentService>();
        services.AddScoped<ISymptomService, SymptomService>();
        services.AddSingleton<IPatientPriorityPredictionEngine, PatientPriorityPredictionEngine>();

        services.AddScoped<IValidator<ClassifyPatientPriorityRequestDto>, ClassifyPatientPriorityRequestValidator>();
        services.AddScoped<IValidator<OverridePatientPriorityRequestDto>, OverridePatientPriorityRequestValidator>();
        services.AddScoped<IValidator<RegisterRequestDto>, RegisterRequestValidator>();
        services.AddScoped<IValidator<LoginRequestDto>, LoginRequestValidator>();
        services.AddScoped<IValidator<UpdateProfileRequestDto>, UpdateProfileRequestValidator>();
        services.AddScoped<IValidator<ChangePasswordRequestDto>, ChangePasswordRequestValidator>();
        services.AddScoped<IValidator<ForgotPasswordRequestDto>, ForgotPasswordRequestValidator>();
        services.AddScoped<IValidator<ResetPasswordRequestDto>, ResetPasswordRequestValidator>();
        services.AddScoped<IValidator<AdminCreateUserRequestDto>, AdminCreateUserRequestValidator>();
        services.AddScoped<IValidator<CreatePatientProfileRequestDto>, CreatePatientProfileRequestValidator>();
        services.AddScoped<IValidator<UpdatePatientProfileRequestDto>, UpdatePatientProfileRequestValidator>();
        services.AddScoped<IValidator<CreateEmergencyContactRequestDto>, CreateEmergencyContactRequestValidator>();
        services.AddScoped<IValidator<CreateMedicalHistoryRequestDto>, CreateMedicalHistoryRequestValidator>();
        services.AddScoped<IValidator<UploadDocumentRequestDto>, UploadDocumentRequestValidator>();
        services.AddScoped<IValidator<CreateDoctorProfileRequestDto>, CreateDoctorProfileRequestValidator>();
        services.AddScoped<IValidator<UpdateDoctorProfileRequestDto>, UpdateDoctorProfileRequestValidator>();
        services.AddScoped<IValidator<CreateDoctorScheduleRequestDto>, CreateDoctorScheduleRequestValidator>();
        services.AddScoped<IValidator<CreateSpecializationRequestDto>, CreateSpecializationRequestValidator>();
        services.AddScoped<IValidator<UpdateSpecializationRequestDto>, UpdateSpecializationRequestValidator>();
        services.AddScoped<IValidator<CreateAppointmentRequestDto>, CreateAppointmentRequestValidator>();
        services.AddScoped<IValidator<UpdateAppointmentStatusRequestDto>, UpdateAppointmentStatusRequestValidator>();
        services.AddScoped<IValidator<SubmitSymptomsRequestDto>, SubmitSymptomsRequestValidator>();

        return services;
    }
}
