-- =============================================
-- Script: Insert/006_Seed_Admin_User.sql
-- Password: Admin@123 (BCrypt hash)
-- =============================================

USE [AppointmentBooking];
GO

DECLARE @AdminRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Admin');

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin')
BEGIN
    INSERT INTO dbo.Users (Username, Email, PasswordHash, FullName, IsActive)
    VALUES (
        N'admin',
        N'admin@clinic.com',
        N'$2a$11$xuzIYk9J8Ji7RIhkYp5aEODt/njYRoHNv7ktGdE8l5q0HxZ9Wn2vG',
        N'System Administrator',
        1
    );

    DECLARE @AdminUserId INT = SCOPE_IDENTITY();

    IF @AdminRoleId IS NOT NULL
        INSERT INTO dbo.UserRoles (UserId, RoleId) VALUES (@AdminUserId, @AdminRoleId);
END
GO
