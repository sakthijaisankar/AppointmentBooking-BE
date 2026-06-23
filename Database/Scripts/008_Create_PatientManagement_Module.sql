-- =============================================
-- Script: 008_Create_PatientManagement_Module.sql
-- Module: Patient Management — medical history, contacts, documents
-- Dependencies: Patients, Users
-- =============================================

USE [AppointmentBooking];
GO

-- Migrate existing Patients table (from earlier schema without UserId)
IF COL_LENGTH(N'dbo.Patients', N'UserId') IS NULL
BEGIN
    ALTER TABLE dbo.Patients ADD UserId INT NULL;

    -- Drop legacy FK if exists
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Patients_Users_CreatedBy')
        ALTER TABLE dbo.Patients DROP CONSTRAINT FK_Patients_Users_CreatedBy;

    IF COL_LENGTH(N'dbo.Patients', N'CreatedByUserId') IS NOT NULL
        ALTER TABLE dbo.Patients DROP COLUMN CreatedByUserId;

    IF COL_LENGTH(N'dbo.Patients', N'EmergencyContact') IS NOT NULL
        ALTER TABLE dbo.Patients DROP COLUMN EmergencyContact;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Patients_Users')
BEGIN
    ALTER TABLE dbo.Patients
    ADD CONSTRAINT FK_Patients_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Patients_UserId' AND object_id = OBJECT_ID(N'dbo.Patients'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Patients_UserId ON dbo.Patients (UserId) WHERE UserId IS NOT NULL;
END
GO

-- Patient Medical History
IF OBJECT_ID(N'dbo.PatientMedicalHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientMedicalHistory
    (
        PatientMedicalHistoryId INT             IDENTITY(1,1) NOT NULL,
        PatientId               INT             NOT NULL,
        ConditionName           NVARCHAR(200)   NOT NULL,
        DiagnosisDate           DATE            NULL,
        Description             NVARCHAR(1000)  NULL,
        IsChronic               BIT             NOT NULL CONSTRAINT DF_PatientMedicalHistory_IsChronic DEFAULT (0),
        IsActive                BIT             NOT NULL CONSTRAINT DF_PatientMedicalHistory_IsActive DEFAULT (1),
        CreatedAt               DATETIME2(7)    NOT NULL CONSTRAINT DF_PatientMedicalHistory_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt               DATETIME2(7)    NULL,
        CONSTRAINT PK_PatientMedicalHistory PRIMARY KEY CLUSTERED (PatientMedicalHistoryId),
        CONSTRAINT FK_PatientMedicalHistory_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients (PatientId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_PatientMedicalHistory_PatientId ON dbo.PatientMedicalHistory (PatientId);
END
GO

-- Emergency Contacts
IF OBJECT_ID(N'dbo.EmergencyContacts', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.EmergencyContacts
    (
        EmergencyContactId      INT             IDENTITY(1,1) NOT NULL,
        PatientId               INT             NOT NULL,
        ContactName             NVARCHAR(200)   NOT NULL,
        Relationship            NVARCHAR(100)   NOT NULL,
        PhoneNumber             NVARCHAR(20)    NOT NULL,
        Email                   NVARCHAR(256)   NULL,
        IsPrimary               BIT             NOT NULL CONSTRAINT DF_EmergencyContacts_IsPrimary DEFAULT (0),
        IsActive                BIT             NOT NULL CONSTRAINT DF_EmergencyContacts_IsActive DEFAULT (1),
        CreatedAt               DATETIME2(7)    NOT NULL CONSTRAINT DF_EmergencyContacts_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt               DATETIME2(7)    NULL,
        CONSTRAINT PK_EmergencyContacts PRIMARY KEY CLUSTERED (EmergencyContactId),
        CONSTRAINT FK_EmergencyContacts_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients (PatientId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_EmergencyContacts_PatientId ON dbo.EmergencyContacts (PatientId);
END
GO

-- Patient Documents
IF OBJECT_ID(N'dbo.PatientDocuments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientDocuments
    (
        PatientDocumentId       INT             IDENTITY(1,1) NOT NULL,
        PatientId               INT             NOT NULL,
        DocumentName            NVARCHAR(255)   NOT NULL,
        DocumentType            NVARCHAR(50)    NOT NULL,
        StoredFileName          NVARCHAR(500)   NOT NULL,
        FilePath                NVARCHAR(1000)  NOT NULL,
        ContentType             NVARCHAR(100)   NOT NULL,
        FileSizeBytes           BIGINT          NOT NULL,
        UploadedByUserId        INT             NOT NULL,
        UploadedAt              DATETIME2(7)    NOT NULL CONSTRAINT DF_PatientDocuments_UploadedAt DEFAULT (SYSUTCDATETIME()),
        IsActive                BIT             NOT NULL CONSTRAINT DF_PatientDocuments_IsActive DEFAULT (1),
        CONSTRAINT PK_PatientDocuments PRIMARY KEY CLUSTERED (PatientDocumentId),
        CONSTRAINT FK_PatientDocuments_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients (PatientId) ON DELETE CASCADE,
        CONSTRAINT FK_PatientDocuments_Users FOREIGN KEY (UploadedByUserId) REFERENCES dbo.Users (UserId)
    );

    CREATE NONCLUSTERED INDEX IX_PatientDocuments_PatientId ON dbo.PatientDocuments (PatientId);
END
GO
