-- =============================================
-- Script: Update/001_Update_PatientPriority_SetCurrent.sql
-- Description: Mark previous classifications as not current when new one is inserted
-- Usage: Called from application layer or trigger alternative
-- =============================================

USE [AppointmentBooking];
GO

CREATE OR ALTER PROCEDURE dbo.usp_PatientPriority_SetCurrent
    @PatientId INT,
    @NewClassificationId INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.PatientPriorityClassifications
    SET IsCurrent = 0
    WHERE PatientId = @PatientId
      AND PatientPriorityClassificationId <> @NewClassificationId
      AND IsCurrent = 1;

    UPDATE dbo.PatientPriorityClassifications
    SET IsCurrent = 1
    WHERE PatientPriorityClassificationId = @NewClassificationId;
END
GO
