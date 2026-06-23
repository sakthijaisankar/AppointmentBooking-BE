-- =============================================
-- Script: 005_Create_Appointments.sql
-- Description: Appointment booking table (links patients, doctors, priority)
-- =============================================

USE [AppointmentBooking];
GO

IF OBJECT_ID(N'dbo.AppointmentStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AppointmentStatuses
    (
        AppointmentStatusId INT             IDENTITY(1,1) NOT NULL,
        StatusName          NVARCHAR(50)    NOT NULL,
        Description         NVARCHAR(200)   NULL,
        CONSTRAINT PK_AppointmentStatuses PRIMARY KEY CLUSTERED (AppointmentStatusId),
        CONSTRAINT UQ_AppointmentStatuses_StatusName UNIQUE (StatusName)
    );
END
GO

IF OBJECT_ID(N'dbo.Appointments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Appointments
    (
        AppointmentId           INT             IDENTITY(1,1) NOT NULL,
        AppointmentNumber       NVARCHAR(30)    NOT NULL,
        PatientId               INT             NOT NULL,
        DoctorId                INT             NOT NULL,
        ClinicId                INT             NOT NULL,
        AppointmentStatusId     INT             NOT NULL,
        ScheduledDateTime       DATETIME2(7)    NOT NULL,
        ReasonForVisit          NVARCHAR(500)   NULL,
        Notes                   NVARCHAR(1000)  NULL,
        CreatedByUserId         INT             NULL,
        CreatedAt               DATETIME2(7)    NOT NULL CONSTRAINT DF_Appointments_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt               DATETIME2(7)    NULL,
        CONSTRAINT PK_Appointments PRIMARY KEY CLUSTERED (AppointmentId),
        CONSTRAINT UQ_Appointments_AppointmentNumber UNIQUE (AppointmentNumber),
        CONSTRAINT FK_Appointments_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients (PatientId),
        CONSTRAINT FK_Appointments_Doctors FOREIGN KEY (DoctorId) REFERENCES dbo.Doctors (DoctorId),
        CONSTRAINT FK_Appointments_Clinics FOREIGN KEY (ClinicId) REFERENCES dbo.Clinics (ClinicId),
        CONSTRAINT FK_Appointments_AppointmentStatuses FOREIGN KEY (AppointmentStatusId) REFERENCES dbo.AppointmentStatuses (AppointmentStatusId),
        CONSTRAINT FK_Appointments_Users_CreatedBy FOREIGN KEY (CreatedByUserId) REFERENCES dbo.Users (UserId)
    );

    CREATE NONCLUSTERED INDEX IX_Appointments_PatientId ON dbo.Appointments (PatientId);
    CREATE NONCLUSTERED INDEX IX_Appointments_ScheduledDateTime ON dbo.Appointments (ScheduledDateTime);
END
GO
