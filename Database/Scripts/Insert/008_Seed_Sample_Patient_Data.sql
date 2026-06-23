-- =============================================
-- Script: Insert/008_Seed_Sample_Patient_Data.sql
-- Sample medical history, emergency contact for PAT-00001
-- =============================================

USE [AppointmentBooking];
GO

DECLARE @PatientId INT = (SELECT PatientId FROM dbo.Patients WHERE PatientCode = N'PAT-00001');

IF @PatientId IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.EmergencyContacts WHERE PatientId = @PatientId)
        INSERT INTO dbo.EmergencyContacts (PatientId, ContactName, Relationship, PhoneNumber, Email, IsPrimary)
        VALUES (@PatientId, N'Jane Patient', N'Spouse', N'+1-555-0099', N'jane.patient@email.com', 1);

    IF NOT EXISTS (SELECT 1 FROM dbo.PatientMedicalHistory WHERE PatientId = @PatientId)
        INSERT INTO dbo.PatientMedicalHistory (PatientId, ConditionName, DiagnosisDate, Description, IsChronic)
        VALUES (@PatientId, N'Hypertension', '2020-03-10', N'Managed with medication', 1);
END
GO
