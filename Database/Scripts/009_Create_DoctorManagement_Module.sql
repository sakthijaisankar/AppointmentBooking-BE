-- =============================================
-- Script: 009_Create_DoctorManagement_Module.sql
-- Module: Doctor Management (Module 3)
-- Description: Adds Specializations and DoctorSchedules tables, and updates Doctors table.
-- =============================================

USE [AppointmentBooking];
GO

-- 1. Create Specializations table
IF OBJECT_ID(N'dbo.Specializations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Specializations
    (
        SpecializationId   INT             IDENTITY(1,1) NOT NULL,
        SpecializationName NVARCHAR(100)    NOT NULL,
        Description        NVARCHAR(500)   NULL,
        IsActive           BIT             NOT NULL CONSTRAINT DF_Specializations_IsActive DEFAULT (1),
        CreatedAt          DATETIME2(7)    NOT NULL CONSTRAINT DF_Specializations_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Specializations PRIMARY KEY CLUSTERED (SpecializationId),
        CONSTRAINT UQ_Specializations_SpecializationName UNIQUE (SpecializationName)
    );
END
GO

-- 2. Seed default specializations if empty
IF NOT EXISTS (SELECT 1 FROM dbo.Specializations)
BEGIN
    INSERT INTO dbo.Specializations (SpecializationName, Description)
    VALUES 
    (N'General Medicine', N'Primary family care and general health consultations'),
    (N'Cardiology', N'Heart and cardiovascular system care'),
    (N'Pediatrics', N'Infant, child, and adolescent healthcare'),
    (N'Dermatology', N'Skin, hair, and nail treatments'),
    (N'Orthopedics', N'Musculoskeletal system, bones, joints, ligaments and muscles'),
    (N'Neurology', N'Brain, spinal cord and nervous system disorders');
END
GO

-- 3. Add SpecializationId to Doctors table if it doesn't exist
IF COL_LENGTH(N'dbo.Doctors', N'SpecializationId') IS NULL
BEGIN
    ALTER TABLE dbo.Doctors ADD SpecializationId INT NULL;
END
GO

-- 4. Add FK constraint if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Doctors_Specializations')
BEGIN
    ALTER TABLE dbo.Doctors ADD CONSTRAINT FK_Doctors_Specializations
        FOREIGN KEY (SpecializationId) REFERENCES dbo.Specializations (SpecializationId);
END
GO

-- 5. Migrate existing data from string Specialization to SpecializationId if the column exists
IF COL_LENGTH(N'dbo.Doctors', N'Specialization') IS NOT NULL
BEGIN
    -- Temporary update from existing string matching
    UPDATE d
    SET d.SpecializationId = s.SpecializationId
    FROM dbo.Doctors d
    INNER JOIN dbo.Specializations s ON LOWER(TRIM(d.Specialization)) = LOWER(TRIM(s.SpecializationName));

    -- Fallback for unmatched specializations to General Medicine
    DECLARE @GenMedId INT = (SELECT SpecializationId FROM dbo.Specializations WHERE SpecializationName = N'General Medicine');
    UPDATE dbo.Doctors SET SpecializationId = @GenMedId WHERE SpecializationId IS NULL;

    -- Make SpecializationId NOT NULL
    ALTER TABLE dbo.Doctors ALTER COLUMN SpecializationId INT NOT NULL;

    -- Drop legacy string Specialization column
    ALTER TABLE dbo.Doctors DROP COLUMN Specialization;
END
GO

-- 6. Create DoctorSchedules table
IF OBJECT_ID(N'dbo.DoctorSchedules', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DoctorSchedules
    (
        DoctorScheduleId    INT             IDENTITY(1,1) NOT NULL,
        DoctorId            INT             NOT NULL,
        DayOfWeek           INT             NOT NULL, -- 0 = Sunday, 1 = Monday, ..., 6 = Saturday (matching System.DayOfWeek)
        StartTime           TIME            NOT NULL,
        EndTime             TIME            NOT NULL,
        SlotDurationMinutes INT             NOT NULL CONSTRAINT DF_DoctorSchedules_SlotDurationMinutes DEFAULT (15),
        IsActive            BIT             NOT NULL CONSTRAINT DF_DoctorSchedules_IsActive DEFAULT (1),
        CreatedAt           DATETIME2(7)    NOT NULL CONSTRAINT DF_DoctorSchedules_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UpdatedAt           DATETIME2(7)    NULL,
        CONSTRAINT PK_DoctorSchedules PRIMARY KEY CLUSTERED (DoctorScheduleId),
        CONSTRAINT FK_DoctorSchedules_Doctors FOREIGN KEY (DoctorId) REFERENCES dbo.Doctors (DoctorId) ON DELETE CASCADE,
        CONSTRAINT CK_DoctorSchedules_DayOfWeek CHECK (DayOfWeek >= 0 AND DayOfWeek <= 6),
        CONSTRAINT CK_DoctorSchedules_TimeRange CHECK (StartTime < EndTime)
    );

    CREATE NONCLUSTERED INDEX IX_DoctorSchedules_DoctorId ON dbo.DoctorSchedules (DoctorId);
END
GO
