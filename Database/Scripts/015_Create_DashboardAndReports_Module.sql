USE [AppointmentBooking];
GO

-- 1. CREATE DEPARTMENTS TABLE
IF OBJECT_ID(N'dbo.Departments', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Departments
    (
        DepartmentId   INT             IDENTITY(1,1) NOT NULL,
        DepartmentName NVARCHAR(100)    NOT NULL,
        Description    NVARCHAR(500)   NULL,
        IsActive       BIT             NOT NULL CONSTRAINT DF_Departments_IsActive DEFAULT (1),
        CreatedAt      DATETIME2(7)    NOT NULL CONSTRAINT DF_Departments_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_Departments PRIMARY KEY CLUSTERED (DepartmentId),
        CONSTRAINT UQ_Departments_DepartmentName UNIQUE (DepartmentName)
    );

    -- Seed default departments
    INSERT INTO dbo.Departments (DepartmentName, Description)
    VALUES 
    (N'General Medicine', N'Primary family care and general health consultations'),
    (N'Cardiology', N'Heart and cardiovascular system care'),
    (N'Pediatrics', N'Infant, child, and adolescent healthcare'),
    (N'Dermatology', N'Skin, hair, and nail treatments'),
    (N'Orthopedics', N'Musculoskeletal system, bones, joints, ligaments and muscles'),
    (N'Neurology', N'Brain, spinal cord and nervous system disorders');
END;
GO

-- 2. UPDATE SPECIALIZATIONS TABLE TO REFERENCE DEPARTMENTS
IF COL_LENGTH(N'dbo.Specializations', N'DepartmentId') IS NULL
BEGIN
    ALTER TABLE dbo.Specializations ADD DepartmentId INT NULL;
    
    ALTER TABLE dbo.Specializations ADD CONSTRAINT FK_Specializations_Departments
        FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments (DepartmentId);
END;
GO

-- Map default specializations to their respective departments
UPDATE s
SET s.DepartmentId = d.DepartmentId
FROM dbo.Specializations s
INNER JOIN dbo.Departments d ON s.SpecializationName = d.DepartmentName;

-- Set default General Medicine department for any unmapped specialization, then make column NOT NULL
DECLARE @GenMedId INT = (SELECT DepartmentId FROM dbo.Departments WHERE DepartmentName = N'General Medicine');
UPDATE dbo.Specializations SET DepartmentId = @GenMedId WHERE DepartmentId IS NULL;

ALTER TABLE dbo.Specializations ALTER COLUMN DepartmentId INT NOT NULL;
GO

-- 3. CREATE SYSTEMSETTINGS TABLE
IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemSettings
    (
        SettingId    INT             IDENTITY(1,1) NOT NULL,
        SettingKey   NVARCHAR(100)   NOT NULL,
        SettingValue NVARCHAR(MAX)   NOT NULL,
        Description  NVARCHAR(500)   NULL,
        UpdatedAt    DATETIME2(7)    NOT NULL CONSTRAINT DF_SystemSettings_UpdatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_SystemSettings PRIMARY KEY CLUSTERED (SettingId),
        CONSTRAINT UQ_SystemSettings_SettingKey UNIQUE (SettingKey)
    );

    -- Seed settings
    INSERT INTO dbo.SystemSettings (SettingKey, SettingValue, Description)
    VALUES 
    (N'ClinicName', N'Healwell Medical Clinic', N'Official name of the medical clinic'),
    (N'OpeningTime', N'08:00', N'Clinic opening hours (HH:mm)'),
    (N'ClosingTime', N'20:00', N'Clinic closing hours (HH:mm)'),
    (N'MaxDailyAppointmentsPerDoctor', N'20', N'Limit on maximum bookings allowed for a practitioner in one day');
END;
GO

-- 4. CREATE AUDITLOGS TABLE
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuditLogs
    (
        AuditLogId   INT             IDENTITY(1,1) NOT NULL,
        UserId       INT             NULL,
        Action       NVARCHAR(100)   NOT NULL,
        EntityName   NVARCHAR(100)   NOT NULL,
        EntityId     INT             NULL,
        Details      NVARCHAR(MAX)   NULL,
        IpAddress    NVARCHAR(50)    NULL,
        Timestamp    DATETIME2(7)    NOT NULL CONSTRAINT DF_AuditLogs_Timestamp DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (AuditLogId),
        CONSTRAINT FK_AuditLogs_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId) ON DELETE SET NULL
    );
END;
GO

-- 5. STORED PROCEDURES FOR ANALYTICS AND DASHBOARD

-- SP 1: Dashboard summary count KPIs
IF OBJECT_ID(N'dbo.sp_GetDashboardSummaryStats', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetDashboardSummaryStats;
GO

CREATE PROCEDURE dbo.sp_GetDashboardSummaryStats
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalPatients INT = (SELECT COUNT(*) FROM dbo.Patients);
    
    DECLARE @AppointmentsToday INT = (
        SELECT COUNT(*) 
        FROM dbo.Appointments 
        WHERE CAST(ScheduledDateTime AS DATE) = CAST(SYSUTCDATETIME() AS DATE)
    );

    DECLARE @ActiveQueueCount INT = (
        SELECT COUNT(*) 
        FROM dbo.QueueManagement q
        INNER JOIN dbo.QueueStatuses s ON q.QueueStatusId = s.QueueStatusId
        WHERE s.StatusCode IN (N'WAITING', N'CALLING', N'IN_CONSULTATION')
    );

    DECLARE @AvgWaitTimeMinutes INT = (
        SELECT ISNULL(AVG(q.EstimatedWaitTimeMinutes), 0)
        FROM dbo.QueueManagement q
        INNER JOIN dbo.QueueStatuses s ON q.QueueStatusId = s.QueueStatusId
        WHERE s.StatusCode IN (N'WAITING', N'CALLING')
    );

    SELECT 
        @TotalPatients AS TotalPatients,
        @AppointmentsToday AS AppointmentsToday,
        @ActiveQueueCount AS ActiveQueueCount,
        @AvgWaitTimeMinutes AS AvgWaitTimeMinutes;
END;
GO

-- SP 2: Appointment statistics (Trend & status distribution)
IF OBJECT_ID(N'dbo.sp_GetAppointmentReport', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetAppointmentReport;
GO

CREATE PROCEDURE dbo.sp_GetAppointmentReport
AS
BEGIN
    SET NOCOUNT ON;

    -- Return Status Distribution
    SELECT s.StatusName, COUNT(a.AppointmentId) AS Count
    FROM dbo.AppointmentStatuses s
    LEFT JOIN dbo.Appointments a ON a.AppointmentStatusId = s.AppointmentStatusId
    GROUP BY s.StatusName;

    -- Return 12-Month Booking Volume Trend
    SELECT 
        YEAR(a.ScheduledDateTime) AS Year, 
        MONTH(a.ScheduledDateTime) AS Month, 
        COUNT(a.AppointmentId) AS Volume
    FROM dbo.Appointments a
    WHERE a.ScheduledDateTime >= DATEADD(month, -12, SYSUTCDATETIME())
    GROUP BY YEAR(a.ScheduledDateTime), MONTH(a.ScheduledDateTime)
    ORDER BY Year DESC, Month DESC;
END;
GO

-- SP 3: Queue & emergency classification
IF OBJECT_ID(N'dbo.sp_GetQueueAndEmergencyReport', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetQueueAndEmergencyReport;
GO

CREATE PROCEDURE dbo.sp_GetQueueAndEmergencyReport
AS
BEGIN
    SET NOCOUNT ON;

    -- Triage Levels Distribution
    SELECT p.LevelName, COUNT(c.PatientPriorityClassificationId) AS Count
    FROM dbo.PriorityLevels p
    LEFT JOIN dbo.PatientPriorityClassifications c ON c.PredictedPriorityLevelId = p.PriorityLevelId
    GROUP BY p.LevelName;

    -- Active overrides log count
    SELECT COUNT(*) AS OverrideCount 
    FROM dbo.PriorityClassificationOverrides;
END;
GO

-- SP 4: Doctor Performance
IF OBJECT_ID(N'dbo.sp_GetDoctorPerformanceReport', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetDoctorPerformanceReport;
GO

CREATE PROCEDURE dbo.sp_GetDoctorPerformanceReport
AS
BEGIN
    SET NOCOUNT ON;

    SELECT 
        d.DoctorId,
        (d.FirstName + N' ' + d.LastName) AS DoctorName,
        s.SpecializationName,
        COUNT(c.ConsultationId) AS TotalConsultations,
        ISNULL(AVG(DATEDIFF(minute, q.CheckInTime, q.ConsultationStartTime)), 0) AS AvgWaitTimeMinutes
    FROM dbo.Doctors d
    INNER JOIN dbo.Specializations s ON d.SpecializationId = s.SpecializationId
    LEFT JOIN dbo.Consultations c ON d.DoctorId = c.DoctorId
    LEFT JOIN dbo.QueueManagement q ON c.AppointmentId = q.AppointmentId
    GROUP BY d.DoctorId, d.FirstName, d.LastName, s.SpecializationName;
END;
GO

-- SP 5: Patient Demographics & Analytics
IF OBJECT_ID(N'dbo.sp_GetPatientAnalyticsReport', N'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_GetPatientAnalyticsReport;
GO

CREATE PROCEDURE dbo.sp_GetPatientAnalyticsReport
AS
BEGIN
    SET NOCOUNT ON;

    -- Age Group breakdown
    SELECT 
        CASE 
            WHEN DATEDIFF(year, DateOfBirth, SYSUTCDATETIME()) < 18 THEN N'Under 18'
            WHEN DATEDIFF(year, DateOfBirth, SYSUTCDATETIME()) BETWEEN 18 AND 35 THEN N'18-35'
            WHEN DATEDIFF(year, DateOfBirth, SYSUTCDATETIME()) BETWEEN 36 AND 50 THEN N'36-50'
            WHEN DATEDIFF(year, DateOfBirth, SYSUTCDATETIME()) BETWEEN 51 AND 65 THEN N'51-65'
            ELSE N'65+'
        END AS AgeGroup,
        COUNT(*) AS Count
    FROM dbo.Patients
    GROUP BY 
        CASE 
            WHEN DATEDIFF(year, DateOfBirth, SYSUTCDATETIME()) < 18 THEN N'Under 18'
            WHEN DATEDIFF(year, DateOfBirth, SYSUTCDATETIME()) BETWEEN 18 AND 35 THEN N'18-35'
            WHEN DATEDIFF(year, DateOfBirth, SYSUTCDATETIME()) BETWEEN 36 AND 50 THEN N'36-50'
            WHEN DATEDIFF(year, DateOfBirth, SYSUTCDATETIME()) BETWEEN 51 AND 65 THEN N'51-65'
            ELSE N'65+'
        END;

    -- Gender distribution
    SELECT Gender, COUNT(*) AS Count
    FROM dbo.Patients
    GROUP BY Gender;
END;
GO
