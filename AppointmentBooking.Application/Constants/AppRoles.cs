namespace AppointmentBooking.Application.Constants;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Receptionist = "Receptionist";
    public const string Doctor = "Doctor";
    public const string Patient = "Patient";

    public static readonly string[] All = [Admin, Receptionist, Doctor, Patient];
    public static readonly string[] Staff = [Admin, Receptionist, Doctor];
}

public static class AuthPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string StaffOnly = "StaffOnly";
    public const string DoctorOrAdmin = "DoctorOrAdmin";
}
