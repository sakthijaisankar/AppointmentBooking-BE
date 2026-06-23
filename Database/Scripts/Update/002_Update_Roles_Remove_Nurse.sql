-- =============================================
-- Script: Update/002_Update_Roles_Remove_Nurse.sql
-- Description: Migrate legacy Nurse role to Receptionist if present
-- =============================================

USE [AppointmentBooking];
GO

DECLARE @NurseRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Nurse');
DECLARE @ReceptionistRoleId INT = (SELECT RoleId FROM dbo.Roles WHERE RoleName = N'Receptionist');

IF @NurseRoleId IS NOT NULL AND @ReceptionistRoleId IS NOT NULL
BEGIN
    UPDATE ur
    SET RoleId = @ReceptionistRoleId
    FROM dbo.UserRoles ur
    WHERE ur.RoleId = @NurseRoleId
      AND NOT EXISTS (
          SELECT 1 FROM dbo.UserRoles x
          WHERE x.UserId = ur.UserId AND x.RoleId = @ReceptionistRoleId
      );

    DELETE FROM dbo.UserRoles WHERE RoleId = @NurseRoleId;
    DELETE FROM dbo.Roles WHERE RoleId = @NurseRoleId;
END
GO
