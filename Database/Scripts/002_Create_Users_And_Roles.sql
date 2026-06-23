-- =============================================
-- Script: 002_Create_Users_And_Roles.sql
-- Module: Authentication & Authorization
-- Description: Core auth tables — referenced by all modules via UserId
-- =============================================

USE [AppointmentBooking];
GO

IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Roles
    (
        RoleId          INT             IDENTITY(1,1) NOT NULL,
        RoleName        NVARCHAR(50)    NOT NULL,
        Description     NVARCHAR(200)   NULL,
        IsActive        BIT             NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT (1),
        CreatedAt       DATETIME2(7)    NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Roles PRIMARY KEY CLUSTERED (RoleId),
        CONSTRAINT UQ_Roles_RoleName UNIQUE (RoleName)
    );
END
GO

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId          INT             IDENTITY(1,1) NOT NULL,
        Username        NVARCHAR(100)   NOT NULL,
        Email           NVARCHAR(256)   NOT NULL,
        PasswordHash    NVARCHAR(500)   NOT NULL,
        FullName        NVARCHAR(200)   NOT NULL,
        PhoneNumber     NVARCHAR(20)    NULL,
        IsActive        BIT             NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        CreatedAt       DATETIME2(7)    NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt       DATETIME2(7)    NULL,
        CONSTRAINT PK_Users PRIMARY KEY CLUSTERED (UserId),
        CONSTRAINT UQ_Users_Username UNIQUE (Username),
        CONSTRAINT UQ_Users_Email UNIQUE (Email)
    );
END
GO

IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserRoles
    (
        UserRoleId      INT             IDENTITY(1,1) NOT NULL,
        UserId          INT             NOT NULL,
        RoleId          INT             NOT NULL,
        AssignedAt      DATETIME2(7)    NOT NULL CONSTRAINT DF_UserRoles_AssignedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_UserRoles PRIMARY KEY CLUSTERED (UserRoleId),
        CONSTRAINT UQ_UserRoles_UserId_RoleId UNIQUE (UserId, RoleId),
        CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId) ON DELETE CASCADE,
        CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles (RoleId)
    );
END
GO
