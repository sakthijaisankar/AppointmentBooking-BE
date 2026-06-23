using AppointmentBooking.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace AppointmentBooking.Database;

public class AppointmentBookingDbContext : DbContext
{
    public AppointmentBookingDbContext(DbContextOptions<AppointmentBookingDbContext> options)
        : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientMedicalHistory> PatientMedicalHistories => Set<PatientMedicalHistory>();
    public DbSet<EmergencyContact> EmergencyContacts => Set<EmergencyContact>();
    public DbSet<PatientDocument> PatientDocuments => Set<PatientDocument>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<Specialization> Specializations => Set<Specialization>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<AppointmentStatus> AppointmentStatuses => Set<AppointmentStatus>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<PriorityLevel> PriorityLevels => Set<PriorityLevel>();
    public DbSet<MlModelVersion> MlModelVersions => Set<MlModelVersion>();
    public DbSet<PatientClinicalFeature> PatientClinicalFeatures => Set<PatientClinicalFeature>();
    public DbSet<PatientPriorityClassification> PatientPriorityClassifications => Set<PatientPriorityClassification>();
    public DbSet<PriorityClassificationOverride> PriorityClassificationOverrides => Set<PriorityClassificationOverride>();
    public DbSet<Symptom> Symptoms => Set<Symptom>();
    public DbSet<PatientSymptom> PatientSymptoms => Set<PatientSymptom>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.RoleId);
            entity.Property(e => e.RoleName).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.HasOne(e => e.Patient).WithOne(p => p.User).HasForeignKey<Patient>(p => p.UserId);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.ToTable("PasswordResetTokens");
            entity.HasKey(e => e.PasswordResetTokenId);
            entity.Property(e => e.Token).HasMaxLength(500);
            entity.HasOne(e => e.User).WithMany(u => u.PasswordResetTokens).HasForeignKey(e => e.UserId);
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("UserRoles");
            entity.HasKey(e => e.UserRoleId);
            entity.HasOne(e => e.User).WithMany(u => u.UserRoles).HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Role).WithMany(r => r.UserRoles).HasForeignKey(e => e.RoleId);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.ToTable("Patients");
            entity.HasKey(e => e.PatientId);
            entity.Property(e => e.PatientCode).HasMaxLength(20);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        modelBuilder.Entity<PatientMedicalHistory>(entity =>
        {
            entity.ToTable("PatientMedicalHistory");
            entity.HasKey(e => e.PatientMedicalHistoryId);
            entity.HasOne(e => e.Patient).WithMany(p => p.MedicalHistory).HasForeignKey(e => e.PatientId);
        });

        modelBuilder.Entity<EmergencyContact>(entity =>
        {
            entity.ToTable("EmergencyContacts");
            entity.HasKey(e => e.EmergencyContactId);
            entity.HasOne(e => e.Patient).WithMany(p => p.EmergencyContacts).HasForeignKey(e => e.PatientId);
        });

        modelBuilder.Entity<PatientDocument>(entity =>
        {
            entity.ToTable("PatientDocuments");
            entity.HasKey(e => e.PatientDocumentId);
            entity.HasOne(e => e.Patient).WithMany(p => p.Documents).HasForeignKey(e => e.PatientId);
            entity.HasOne(e => e.UploadedByUser).WithMany().HasForeignKey(e => e.UploadedByUserId);
        });

        modelBuilder.Entity<Clinic>(entity =>
        {
            entity.ToTable("Clinics");
            entity.HasKey(e => e.ClinicId);
        });

        modelBuilder.Entity<Specialization>(entity =>
        {
            entity.ToTable("Specializations");
            entity.HasKey(e => e.SpecializationId);
            entity.Property(e => e.SpecializationName).HasMaxLength(100);
            entity.HasIndex(e => e.SpecializationName).IsUnique();
        });

        modelBuilder.Entity<Doctor>(entity =>
        {
            entity.ToTable("Doctors");
            entity.HasKey(e => e.DoctorId);
            entity.HasOne(e => e.Clinic).WithMany(c => c.Doctors).HasForeignKey(e => e.ClinicId);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Specialization).WithMany(s => s.Doctors).HasForeignKey(e => e.SpecializationId);
        });

        modelBuilder.Entity<DoctorSchedule>(entity =>
        {
            entity.ToTable("DoctorSchedules");
            entity.HasKey(e => e.DoctorScheduleId);
            entity.HasOne(e => e.Doctor).WithMany(d => d.Schedules).HasForeignKey(e => e.DoctorId);
        });

        modelBuilder.Entity<AppointmentStatus>(entity =>
        {
            entity.ToTable("AppointmentStatuses");
            entity.HasKey(e => e.AppointmentStatusId);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.ToTable("Appointments");
            entity.HasKey(e => e.AppointmentId);
            entity.HasOne(e => e.Patient).WithMany().HasForeignKey(e => e.PatientId);
            entity.HasOne(e => e.Doctor).WithMany(d => d.Appointments).HasForeignKey(e => e.DoctorId);
            entity.HasOne(e => e.Clinic).WithMany(c => c.Appointments).HasForeignKey(e => e.ClinicId);
            entity.HasOne(e => e.AppointmentStatus).WithMany(s => s.Appointments).HasForeignKey(e => e.AppointmentStatusId);
            entity.HasOne(e => e.CurrentPriorityClassification).WithMany().HasForeignKey(e => e.CurrentPriorityClassificationId);
        });

        modelBuilder.Entity<PriorityLevel>(entity =>
        {
            entity.ToTable("PriorityLevels");
            entity.HasKey(e => e.PriorityLevelId);
            entity.Property(e => e.LevelCode).HasMaxLength(20);
        });

        modelBuilder.Entity<MlModelVersion>(entity =>
        {
            entity.ToTable("MlModelVersions");
            entity.HasKey(e => e.MlModelVersionId);
        });

        modelBuilder.Entity<PatientClinicalFeature>(entity =>
        {
            entity.ToTable("PatientClinicalFeatures");
            entity.HasKey(e => e.PatientClinicalFeatureId);
            entity.HasOne(e => e.Patient).WithMany(p => p.ClinicalFeatures).HasForeignKey(e => e.PatientId);
            entity.HasOne(e => e.Appointment).WithMany().HasForeignKey(e => e.AppointmentId);
            entity.HasOne(e => e.CapturedByUser).WithMany().HasForeignKey(e => e.CapturedByUserId);
        });

        modelBuilder.Entity<PatientPriorityClassification>(entity =>
        {
            entity.ToTable("PatientPriorityClassifications");
            entity.HasKey(e => e.PatientPriorityClassificationId);
            entity.Property(e => e.ConfidenceScore).HasColumnType("decimal(5,4)");
            entity.Property(e => e.RiskScore).HasColumnType("decimal(8,4)");
            entity.HasOne(e => e.Patient).WithMany(p => p.PriorityClassifications).HasForeignKey(e => e.PatientId);
            entity.HasOne(e => e.PatientClinicalFeature).WithMany(f => f.Classifications).HasForeignKey(e => e.PatientClinicalFeatureId);
            entity.HasOne(e => e.MlModelVersion).WithMany(m => m.Classifications).HasForeignKey(e => e.MlModelVersionId);
            entity.HasOne(e => e.PredictedPriorityLevel).WithMany(l => l.Classifications).HasForeignKey(e => e.PredictedPriorityLevelId);
            entity.HasOne(e => e.ClassifiedByUser).WithMany().HasForeignKey(e => e.ClassifiedByUserId);
        });

        modelBuilder.Entity<PriorityClassificationOverride>(entity =>
        {
            entity.ToTable("PriorityClassificationOverrides");
            entity.HasKey(e => e.PriorityClassificationOverrideId);
            entity.HasOne(e => e.PatientPriorityClassification).WithMany(c => c.Overrides).HasForeignKey(e => e.PatientPriorityClassificationId);
            entity.HasOne(e => e.OriginalPriorityLevel).WithMany().HasForeignKey(e => e.OriginalPriorityLevelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.OverridePriorityLevel).WithMany().HasForeignKey(e => e.OverridePriorityLevelId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.OverriddenByUser).WithMany().HasForeignKey(e => e.OverriddenByUserId);
        });

        modelBuilder.Entity<Symptom>(entity =>
        {
            entity.ToTable("Symptoms");
            entity.HasKey(e => e.SymptomId);
            entity.Property(e => e.SymptomName).HasMaxLength(100);
            entity.HasIndex(e => e.SymptomName).IsUnique();
        });

        modelBuilder.Entity<PatientSymptom>(entity =>
        {
            entity.ToTable("PatientSymptoms");
            entity.HasKey(e => e.PatientSymptomId);
            entity.HasOne(e => e.Appointment).WithMany(a => a.PatientSymptoms).HasForeignKey(e => e.AppointmentId);
            entity.HasOne(e => e.Symptom).WithMany().HasForeignKey(e => e.SymptomId);
            entity.HasIndex(e => new { e.AppointmentId, e.SymptomId }).IsUnique();
        });
    }
}
