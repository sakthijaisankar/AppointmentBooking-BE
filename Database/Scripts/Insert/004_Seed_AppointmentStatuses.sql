-- =============================================
-- Script: Insert/004_Seed_AppointmentStatuses.sql
-- =============================================

USE [AppointmentBooking];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatuses WHERE StatusName = N'Scheduled')
    INSERT INTO dbo.AppointmentStatuses (StatusName, Description) VALUES (N'Scheduled', N'Appointment booked');

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatuses WHERE StatusName = N'Confirmed')
    INSERT INTO dbo.AppointmentStatuses (StatusName, Description) VALUES (N'Confirmed', N'Patient confirmed attendance');

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatuses WHERE StatusName = N'InProgress')
    INSERT INTO dbo.AppointmentStatuses (StatusName, Description) VALUES (N'InProgress', N'Patient is being seen');

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatuses WHERE StatusName = N'Completed')
    INSERT INTO dbo.AppointmentStatuses (StatusName, Description) VALUES (N'Completed', N'Visit completed');

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatuses WHERE StatusName = N'Cancelled')
    INSERT INTO dbo.AppointmentStatuses (StatusName, Description) VALUES (N'Cancelled', N'Appointment cancelled');

IF NOT EXISTS (SELECT 1 FROM dbo.AppointmentStatuses WHERE StatusName = N'NoShow')
    INSERT INTO dbo.AppointmentStatuses (StatusName, Description) VALUES (N'NoShow', N'Patient did not attend');
GO
