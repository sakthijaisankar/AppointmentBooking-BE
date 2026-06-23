-- =============================================
-- Script: 003_Create_Patients.sql
-- Module: Patient Management (Module 2)
-- Dependency: Users (Module 1)
-- Description: Patient profile linked 1:1 to User with Role=Patient
-- =============================================

USE [AppointmentBooking];
GO

IF OBJECT_ID(N'dbo.Patients', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Patients
    (
        PatientId           INT             IDENTITY(1,1) NOT NULL,
        UserId              INT             NOT NULL,
        PatientCode         NVARCHAR(20)    NOT NULL,
        FirstName           NVARCHAR(100)   NOT NULL,
        LastName            NVARCHAR(100)   NOT NULL,
        DateOfBirth         DATE            NOT NULL,
        Gender              NVARCHAR(20)    NOT NULL,
        PhoneNumber         NVARCHAR(20)    NULL,
        Email               NVARCHAR(256)   NULL,
        Address             NVARCHAR(500)   NULL,
        BloodGroup          NVARCHAR(10)    NULL,
        IsActive            BIT             NOT NULL CONSTRAINT DF_Patients_IsActive DEFAULT (1),
        CreatedAt           DATETIME2(7)    NOT NULL CONSTRAINT DF_Patients_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2(7)    NULL,
        CONSTRAINT PK_Patients PRIMARY KEY CLUSTERED (PatientId),
        CONSTRAINT UQ_Patients_PatientCode UNIQUE (PatientCode),
        CONSTRAINT UQ_Patients_UserId UNIQUE (UserId),
        CONSTRAINT FK_Patients_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId)
    );

    CREATE NONCLUSTERED INDEX IX_Patients_LastName_FirstName ON dbo.Patients (LastName, FirstName);
END
GO
