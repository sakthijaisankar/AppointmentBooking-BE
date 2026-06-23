-- =============================================
-- Script: 004_Create_Doctors_And_Clinics.sql
-- Description: Clinic and doctor tables for appointment module
-- =============================================

USE [AppointmentBooking];
GO

IF OBJECT_ID(N'dbo.Clinics', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clinics
    (
        ClinicId        INT             IDENTITY(1,1) NOT NULL,
        ClinicName      NVARCHAR(200)   NOT NULL,
        Address         NVARCHAR(500)   NULL,
        PhoneNumber     NVARCHAR(20)    NULL,
        IsActive        BIT             NOT NULL CONSTRAINT DF_Clinics_IsActive DEFAULT (1),
        CreatedAt       DATETIME2(7)    NOT NULL CONSTRAINT DF_Clinics_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Clinics PRIMARY KEY CLUSTERED (ClinicId)
    );
END
GO

IF OBJECT_ID(N'dbo.Doctors', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Doctors
    (
        DoctorId        INT             IDENTITY(1,1) NOT NULL,
        ClinicId        INT             NOT NULL,
        UserId          INT             NULL,
        FirstName       NVARCHAR(100)   NOT NULL,
        LastName        NVARCHAR(100)   NOT NULL,
        Specialization  NVARCHAR(150)   NOT NULL,
        LicenseNumber   NVARCHAR(50)    NOT NULL,
        IsActive        BIT             NOT NULL CONSTRAINT DF_Doctors_IsActive DEFAULT (1),
        CreatedAt       DATETIME2(7)    NOT NULL CONSTRAINT DF_Doctors_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Doctors PRIMARY KEY CLUSTERED (DoctorId),
        CONSTRAINT UQ_Doctors_LicenseNumber UNIQUE (LicenseNumber),
        CONSTRAINT FK_Doctors_Clinics FOREIGN KEY (ClinicId) REFERENCES dbo.Clinics (ClinicId),
        CONSTRAINT FK_Doctors_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
    );
END
GO
