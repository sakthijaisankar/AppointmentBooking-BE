-- =============================================
-- Script: 010_Update_AppointmentStatuses.sql
-- Module: Appointment Management (Module 4)
-- Description: Adds Pending status to the database.
-- =============================================

USE [AppointmentBooking];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatuses WHERE StatusName = N'Pending')
BEGIN
    INSERT INTO dbo.AppointmentStatuses (StatusName, Description)
    VALUES (N'Pending', N'Appointment is booked and awaiting confirmation');
END
GO
