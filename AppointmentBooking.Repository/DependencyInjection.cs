using AppointmentBooking.Repository.Implementations;
using AppointmentBooking.Repository.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AppointmentBooking.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        services.AddScoped<IPatientPriorityRepository, PatientPriorityRepository>();
        services.AddScoped<IPatientRepository, PatientRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();
        return services;
    }
}
