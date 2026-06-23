-- =============================================
-- EF Core Database First Scaffold Commands
-- Run AFTER executing all SQL scripts (001-006 + Insert scripts)
-- =============================================

/*
Prerequisites:
1. Install EF Core tools globally:
   dotnet tool install --global dotnet-ef

2. Ensure SQL Server is running and scripts are executed in order:
   001_Create_Database.sql
   002_Create_Users_And_Roles.sql
   003_Create_Patients.sql
   004_Create_Doctors_And_Clinics.sql
   005_Create_Appointments.sql
   006_Create_PatientPriority_Module.sql
   Insert/001_Seed_Roles.sql
   Insert/002_Seed_PriorityLevels.sql
   Insert/003_Seed_MlModelVersions.sql
   Insert/004_Seed_AppointmentStatuses.sql
   Insert/005_Seed_Sample_Patient.sql
   Update/001_Update_PatientPriority_SetCurrent.sql

3. Scaffold from AppointmentBooking.Database project folder:

   cd "D:\Sakthi\Appointment Booking System\AppointmentBooking-BE\AppointmentBooking.Database"

   dotnet ef dbcontext scaffold ^
     "Server=localhost;Database=AppointmentBooking;Trusted_Connection=True;TrustServerCertificate=True;" ^
     Microsoft.EntityFrameworkCore.SqlServer ^
     -o Entities/Scaffolded ^
     -c AppointmentBookingDbContextScaffolded ^
     --force ^
     --no-onconfiguring

4. Compare scaffolded entities with hand-crafted entities in Entities/DomainEntities.cs
   and merge any schema drift.

5. Alternative: Update existing DbContext only for new tables:

   dotnet ef dbcontext scaffold ^
     "Server=localhost;Database=AppointmentBooking;Trusted_Connection=True;TrustServerCertificate=True;" ^
     Microsoft.EntityFrameworkCore.SqlServer ^
     -t dbo.PatientPriorityClassifications ^
     -t dbo.PatientClinicalFeatures ^
     -t dbo.PriorityLevels ^
     -t dbo.MlModelVersions ^
     -t dbo.PriorityClassificationOverrides ^
     -o Entities/Scaffolded/Priority ^
     --force

6. Connection string is configured in appsettings.json under DefaultConnection.

Note: This project uses hand-crafted entities aligned with SQL scripts.
      Re-scaffold when schema changes and merge into DomainEntities.cs.
*/
