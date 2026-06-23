-- =============================================
-- Script: 001_Create_Database.sql
-- Description: Create AppointmentBooking database
-- =============================================

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'AppointmentBooking')
BEGIN
    CREATE DATABASE [AppointmentBooking];
END
GO

USE [AppointmentBooking];
GO
