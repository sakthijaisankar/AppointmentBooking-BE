-- =============================================
-- Script: Insert/001_Seed_Roles.sql
-- Module: Authentication — four system roles
-- =============================================

USE [AppointmentBooking];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'Admin')
    INSERT INTO dbo.Roles (RoleName, Description) VALUES (N'Admin', N'System administrator with full access');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'Receptionist')
    INSERT INTO dbo.Roles (RoleName, Description) VALUES (N'Receptionist', N'Front desk staff managing appointments');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'Doctor')
    INSERT INTO dbo.Roles (RoleName, Description) VALUES (N'Doctor', N'Clinical doctor providing care');

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE RoleName = N'Patient')
    INSERT INTO dbo.Roles (RoleName, Description) VALUES (N'Patient', N'Patient booking and managing appointments');
GO
