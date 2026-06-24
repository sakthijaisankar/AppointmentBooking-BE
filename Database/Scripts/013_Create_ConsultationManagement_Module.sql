-- =============================================
-- Script: 013_Create_ConsultationManagement_Module.sql
-- Module: Doctor Consultation Management (Module 8)
-- Dependencies: Appointments, Doctors, Patients, Users
-- =============================================

USE [AppointmentBooking];
GO

-- 1. Consultations table (1:1 with Appointments)
IF OBJECT_ID(N'dbo.Consultations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Consultations
    (
        ConsultationId      INT             IDENTITY(1,1) NOT NULL,
        AppointmentId       INT             NOT NULL,
        DoctorId            INT             NOT NULL,
        PatientId           INT             NOT NULL,
        Diagnosis           NVARCHAR(2000)  NOT NULL,
        ClinicalNotes       NVARCHAR(4000)  NULL,
        FollowUpRequired    BIT             NOT NULL CONSTRAINT DF_Consultations_FollowUpRequired DEFAULT (0),
        FollowUpDate        DATE            NULL,
        ConsultedByUserId   INT             NULL,
        CreatedAt           DATETIME2(7)    NOT NULL CONSTRAINT DF_Consultations_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2(7)    NULL,

        CONSTRAINT PK_Consultations PRIMARY KEY CLUSTERED (ConsultationId),
        CONSTRAINT UQ_Consultations_AppointmentId UNIQUE (AppointmentId),
        CONSTRAINT FK_Consultations_Appointments FOREIGN KEY (AppointmentId)
            REFERENCES dbo.Appointments (AppointmentId),
        CONSTRAINT FK_Consultations_Doctors FOREIGN KEY (DoctorId)
            REFERENCES dbo.Doctors (DoctorId),
        CONSTRAINT FK_Consultations_Patients FOREIGN KEY (PatientId)
            REFERENCES dbo.Patients (PatientId),
        CONSTRAINT FK_Consultations_Users FOREIGN KEY (ConsultedByUserId)
            REFERENCES dbo.Users (UserId)
    );

    CREATE NONCLUSTERED INDEX IX_Consultations_PatientId
        ON dbo.Consultations (PatientId);

    CREATE NONCLUSTERED INDEX IX_Consultations_DoctorId
        ON dbo.Consultations (DoctorId);
END
GO

-- 2. Prescriptions table (1:N under Consultations)
IF OBJECT_ID(N'dbo.Prescriptions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Prescriptions
    (
        PrescriptionId      INT             IDENTITY(1,1) NOT NULL,
        ConsultationId      INT             NOT NULL,
        MedicineName        NVARCHAR(200)   NOT NULL,
        Dosage              NVARCHAR(100)   NOT NULL,
        Frequency           NVARCHAR(100)   NOT NULL,
        DurationDays        INT             NOT NULL,
        Instructions        NVARCHAR(500)   NULL,
        CreatedAt           DATETIME2(7)    NOT NULL CONSTRAINT DF_Prescriptions_CreatedAt DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_Prescriptions PRIMARY KEY CLUSTERED (PrescriptionId),
        CONSTRAINT FK_Prescriptions_Consultations FOREIGN KEY (ConsultationId)
            REFERENCES dbo.Consultations (ConsultationId) ON DELETE CASCADE,
        CONSTRAINT CK_Prescriptions_DurationDays CHECK (DurationDays > 0)
    );

    CREATE NONCLUSTERED INDEX IX_Prescriptions_ConsultationId
        ON dbo.Prescriptions (ConsultationId);
END
GO
