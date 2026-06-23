-- =============================================
-- Script: Insert/005_Seed_Sample_Patient.sql
-- Links sample patient user (Module 1) to patient profile
-- Run after Insert/007_Seed_Sample_Users.sql
-- =============================================

USE [AppointmentBooking];
GO

DECLARE @PatientUserId INT = (SELECT UserId FROM dbo.Users WHERE Username = N'patient');

IF @PatientUserId IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.Patients WHERE UserId = @PatientUserId)
BEGIN
    INSERT INTO dbo.Patients (UserId, PatientCode, FirstName, LastName, DateOfBirth, Gender, PhoneNumber, Email, BloodGroup, IsActive)
    VALUES (@PatientUserId, N'PAT-00001', N'John', N'Patient', '1990-06-15', N'Male', N'+1-555-0004', N'patient@email.com', N'O+', 1);
END
GO
