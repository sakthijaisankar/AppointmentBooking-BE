$scripts = @(
  "001_Create_Database.sql",
  "002_Create_Users_And_Roles.sql",
  "003_Create_Patients.sql",
  "004_Create_Doctors_And_Clinics.sql",
  "005_Create_Appointments.sql",
  "006_Create_PatientPriority_Module.sql",
  "007_Create_PasswordResetTokens.sql",
  "008_Create_PatientManagement_Module.sql",
  "009_Create_DoctorManagement_Module.sql",
  "012_Create_QueueManagement_Module.sql",
  "Insert\001_Seed_Roles.sql",
  "Insert\002_Seed_PriorityLevels.sql",
  "Insert\003_Seed_MlModelVersions.sql",
  "Insert\004_Seed_AppointmentStatuses.sql",
  "Insert\006_Seed_Admin_User.sql",
  "Insert\007_Seed_Sample_Users.sql",
  "Insert\005_Seed_Sample_Patient.sql",
  "Insert\008_Seed_Sample_Patient_Data.sql",
  "Update\001_Update_PatientPriority_SetCurrent.sql",
  "Update\002_Update_Roles_Remove_Nurse.sql"
)

foreach ($s in $scripts) {
  Write-Host "Executing $s..."
  sqlcmd -S localhost -i $s
  if ($LASTEXITCODE -ne 0) {
    Write-Error "Failed to execute $s"
    exit 1
  }
}

Write-Host "Database initialization completed successfully."
