-- =============================================
-- Script: 011_Create_SymptomSubmission_Module.sql
-- Description: Creates Symptoms and PatientSymptoms tables, seeds baseline symptoms
-- =============================================

USE [AppointmentBooking];
GO

-- 1. Create Symptoms table
IF OBJECT_ID(N'dbo.Symptoms', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Symptoms
    (
        SymptomId       INT             IDENTITY(1,1) NOT NULL,
        SymptomName     NVARCHAR(100)   NOT NULL,
        Description     NVARCHAR(500)   NULL,
        IsActive        BIT             NOT NULL CONSTRAINT DF_Symptoms_IsActive DEFAULT (1),
        CreatedAt       DATETIME2(7)    NOT NULL CONSTRAINT DF_Symptoms_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Symptoms PRIMARY KEY CLUSTERED (SymptomId),
        CONSTRAINT UQ_Symptoms_SymptomName UNIQUE (SymptomName)
    );
END
GO

-- 2. Create PatientSymptoms junction table
IF OBJECT_ID(N'dbo.PatientSymptoms', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientSymptoms
    (
        PatientSymptomId   INT             IDENTITY(1,1) NOT NULL,
        AppointmentId      INT             NOT NULL,
        SymptomId          INT             NOT NULL,
        SeverityLevel      INT             NOT NULL,
        ExistingConditions NVARCHAR(1000)  NULL,
        Notes              NVARCHAR(1000)  NULL,
        CreatedAt          DATETIME2(7)    NOT NULL CONSTRAINT DF_PatientSymptoms_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_PatientSymptoms PRIMARY KEY CLUSTERED (PatientSymptomId),
        CONSTRAINT UQ_PatientSymptoms_Appointment_Symptom UNIQUE (AppointmentId, SymptomId),
        CONSTRAINT FK_PatientSymptoms_Appointments FOREIGN KEY (AppointmentId) REFERENCES dbo.Appointments (AppointmentId) ON DELETE CASCADE,
        CONSTRAINT FK_PatientSymptoms_Symptoms FOREIGN KEY (SymptomId) REFERENCES dbo.Symptoms (SymptomId)
    );

    CREATE NONCLUSTERED INDEX IX_PatientSymptoms_AppointmentId ON dbo.PatientSymptoms (AppointmentId);
END
GO

-- 3. Seed default symptoms
IF EXISTS (SELECT 1 FROM dbo.Symptoms)
BEGIN
    PRINT 'Symptoms already seeded.';
END
ELSE
BEGIN
    INSERT INTO dbo.Symptoms (SymptomName, Description)
    VALUES 
    (N'Fever', N'High body temperature, feeling hot or cold, shivering.'),
    (N'Cough', N'Dry or productive cough affecting the airways.'),
    (N'Shortness of Breath', N'Difficulty breathing or feeling winded.'),
    (N'Chest Pain', N'Pain, pressure, or tightness in the chest region.'),
    (N'Headache', N'Pain or throbbing in the head or neck area.'),
    (N'Fatigue', N'Severe tiredness, exhaustion, or low energy levels.'),
    (N'Sore Throat', N'Pain, irritation, or scratchiness in the throat.'),
    (N'Nausea / Vomiting', N'Feeling sick to the stomach or throwing up.'),
    (N'Dizziness', N'Feeling lightheaded, unsteady, or spinning.'),
    (N'Muscle Pain', N'Body aches, joint soreness, or muscle stiffness.');
END
GO
