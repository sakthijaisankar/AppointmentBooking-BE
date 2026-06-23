-- =============================================
-- Script: 012_Create_QueueManagement_Module.sql
-- Module: Queue Management
-- Dependencies: Appointments, PatientPriorityClassifications
-- =============================================

USE [AppointmentBooking];
GO

-- 1. Create QueueStatuses lookup table
IF OBJECT_ID(N'dbo.QueueStatuses', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QueueStatuses
    (
        QueueStatusId       INT             IDENTITY(1,1) NOT NULL,
        StatusCode          NVARCHAR(20)    NOT NULL,
        StatusName          NVARCHAR(50)    NOT NULL,
        Description         NVARCHAR(200)   NULL,
        IsActive            BIT             NOT NULL CONSTRAINT DF_QueueStatuses_IsActive DEFAULT (1),
        CreatedAt           DATETIME2(7)    NOT NULL CONSTRAINT DF_QueueStatuses_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_QueueStatuses PRIMARY KEY CLUSTERED (QueueStatusId),
        CONSTRAINT UQ_QueueStatuses_StatusCode UNIQUE (StatusCode)
    );
END
GO

-- Seed default QueueStatuses
IF NOT EXISTS (SELECT 1 FROM dbo.QueueStatuses)
BEGIN
    INSERT INTO dbo.QueueStatuses (StatusCode, StatusName, Description)
    VALUES 
    (N'WAITING', N'Waiting', N'Patient checked in and waiting to be called.'),
    (N'CALLING', N'Calling', N'Staff is currently calling the patient to the consultation room.'),
    (N'IN_CONSULTATION', N'In Consultation', N'Patient is currently consulting with the doctor.'),
    (N'COMPLETED', N'Completed', N'Consultation completed and patient left.'),
    (N'SKIPPED', N'Skipped', N'Patient did not show up when called.');
END
GO

-- 2. Create QueueManagement table
IF OBJECT_ID(N'dbo.QueueManagement', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.QueueManagement
    (
        QueueId                         INT             IDENTITY(1,1) NOT NULL,
        AppointmentId                   INT             NOT NULL,
        PatientPriorityClassificationId INT             NOT NULL,
        QueueNumber                     NVARCHAR(15)    NOT NULL,
        QueueStatusId                   INT             NOT NULL,
        EstimatedWaitTimeMinutes        INT             NOT NULL CONSTRAINT DF_QueueManagement_EstWait DEFAULT (0),
        CheckInTime                     DATETIME2(7)    NOT NULL CONSTRAINT DF_QueueManagement_CheckIn DEFAULT (SYSUTCDATETIME()),
        CallingTime                     DATETIME2(7)    NULL,
        ConsultationStartTime           DATETIME2(7)    NULL,
        ConsultationEndTime             DATETIME2(7)    NULL,
        CreatedAt                       DATETIME2(7)    NOT NULL CONSTRAINT DF_QueueManagement_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt                       DATETIME2(7)    NULL,
        CONSTRAINT PK_QueueManagement PRIMARY KEY CLUSTERED (QueueId),
        CONSTRAINT UQ_QueueManagement_Appointment UNIQUE (AppointmentId),
        CONSTRAINT FK_QueueManagement_Appointments FOREIGN KEY (AppointmentId) REFERENCES dbo.Appointments (AppointmentId) ON DELETE CASCADE,
        CONSTRAINT FK_QueueManagement_Classifications FOREIGN KEY (PatientPriorityClassificationId) REFERENCES dbo.PatientPriorityClassifications (PatientPriorityClassificationId),
        CONSTRAINT FK_QueueManagement_QueueStatuses FOREIGN KEY (QueueStatusId) REFERENCES dbo.QueueStatuses (QueueStatusId)
    );

    CREATE NONCLUSTERED INDEX IX_QueueManagement_QueueStatusId ON dbo.QueueManagement (QueueStatusId);
END
GO
