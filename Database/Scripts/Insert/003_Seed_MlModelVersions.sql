-- =============================================
-- Script: Insert/003_Seed_MlModelVersions.sql
-- =============================================

USE [AppointmentBooking];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.MlModelVersions WHERE ModelName = N'PatientPriorityClassifier' AND VersionNumber = N'1.0.0')
    INSERT INTO dbo.MlModelVersions (ModelName, VersionNumber, ModelPath, AlgorithmType, AccuracyScore, IsActive, Notes)
    VALUES (
        N'PatientPriorityClassifier',
        N'1.0.0',
        N'ML/Models/patient-priority-v1.zip',
        N'ML.NET_SdcaMaximumEntropy',
        0.8720,
        1,
        N'Initial production model trained on synthetic clinical triage dataset'
    );
GO
