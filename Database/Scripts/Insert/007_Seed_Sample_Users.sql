-- =============================================
-- Script: Insert/007_Seed_Sample_Users.sql
-- Password for all sample users: Admin@123
-- BCrypt hash: $2a$11$xuzIYk9J8Ji7RIhkYp5aEODt/njYRoHNv7ktGdE8l5q0HxZ9Wn2vG
-- =============================================

USE [AppointmentBooking];
GO

DECLARE @Hash NVARCHAR(500) = N'$2a$11$xuzIYk9J8Ji7RIhkYp5aEODt/njYRoHNv7ktGdE8l5q0HxZ9Wn2vG';

DECLARE @AdminRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Admin');
DECLARE @ReceptionistRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Receptionist');
DECLARE @DoctorRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Doctor');
DECLARE @PatientRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Patient');

-- Admin
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
BEGIN
    INSERT INTO dbo.Users (Username, Email, PasswordHash, FullName, PhoneNumber, IsActive)
    VALUES (N'admin', N'admin@clinic.com', @Hash, N'System Administrator', N'+1-555-0001', 1);
    INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (SCOPE_IDENTITY(), @AdminRoleId);
END

-- Receptionist
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'receptionist')
BEGIN
    INSERT INTO dbo.Users (Username, Email, PasswordHash, FullName, PhoneNumber, IsActive)
    VALUES (N'receptionist', N'reception@clinic.com', @Hash, N'Jane Receptionist', N'+1-555-0002', 1);
    INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (SCOPE_IDENTITY(), @ReceptionistRoleId);
END

-- Doctor
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'doctor')
BEGIN
    INSERT INTO dbo.Users (Username, Email, PasswordHash, FullName, PhoneNumber, IsActive)
    VALUES (N'doctor', N'doctor@clinic.com', @Hash, N'Dr. Sarah Smith', N'+1-555-0003', 1);
    INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (SCOPE_IDENTITY(), @DoctorRoleId);
END

-- Patient user
IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'patient')
BEGIN
    INSERT INTO dbo.Users (Username, Email, PasswordHash, FullName, PhoneNumber, IsActive)
    VALUES (N'patient', N'patient@email.com', @Hash, N'John Patient', N'+1-555-0004', 1);
    INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (SCOPE_IDENTITY(), @PatientRoleId);
END
GO
