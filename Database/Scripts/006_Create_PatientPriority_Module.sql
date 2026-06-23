-- =============================================
-- Script: 006_Create_PatientPriority_Module.sql
-- Module: ML-based Patient Priority Classification
-- Dependencies: Patients, Users, Appointments
-- =============================================

USE [AppointmentBooking];
GO

-- Lookup: Priority levels used by ML output and queue sorting
IF OBJECT_ID(N'dbo.PriorityLevels', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PriorityLevels
    (
        PriorityLevelId     INT             IDENTITY(1,1) NOT NULL,
        LevelCode           NVARCHAR(20)    NOT NULL,
        LevelName           NVARCHAR(50)    NOT NULL,
        SortOrder           INT             NOT NULL,
        ColorHex            NVARCHAR(7)     NOT NULL,
        Description         NVARCHAR(300)   NULL,
        IsActive            BIT             NOT NULL CONSTRAINT DF_PriorityLevels_IsActive DEFAULT (1),
        CONSTRAINT PK_PriorityLevels PRIMARY KEY CLUSTERED (PriorityLevelId),
        CONSTRAINT UQ_PriorityLevels_LevelCode UNIQUE (LevelCode)
    );
END
GO

-- ML model version registry
IF OBJECT_ID(N'dbo.MlModelVersions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MlModelVersions
    (
        MlModelVersionId    INT             IDENTITY(1,1) NOT NULL,
        ModelName           NVARCHAR(100)   NOT NULL,
        VersionNumber       NVARCHAR(20)    NOT NULL,
        ModelPath           NVARCHAR(500)   NULL,
        AlgorithmType       NVARCHAR(50)    NOT NULL,
        AccuracyScore       DECIMAL(5,4)    NULL,
        IsActive            BIT             NOT NULL CONSTRAINT DF_MlModelVersions_IsActive DEFAULT (1),
        DeployedAt          DATETIME2(7)    NOT NULL CONSTRAINT DF_MlModelVersions_DeployedAt DEFAULT (SYSUTCDATETIME()),
        Notes               NVARCHAR(500)   NULL,
        CONSTRAINT PK_MlModelVersions PRIMARY KEY CLUSTERED (MlModelVersionId),
        CONSTRAINT UQ_MlModelVersions_ModelName_Version UNIQUE (ModelName, VersionNumber)
    );
END
GO

-- Clinical input features captured at classification time
IF OBJECT_ID(N'dbo.PatientClinicalFeatures', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientClinicalFeatures
    (
        PatientClinicalFeatureId    INT             IDENTITY(1,1) NOT NULL,
        PatientId                   INT             NOT NULL,
        AppointmentId               INT             NULL,
        Age                         INT             NOT NULL,
        Gender                      NVARCHAR(20)    NOT NULL,
        HeartRate                   INT             NULL,
        BloodPressureSystolic       INT             NULL,
        BloodPressureDiastolic      INT             NULL,
        TemperatureCelsius          DECIMAL(4,1)    NULL,
        OxygenSaturation            DECIMAL(5,2)    NULL,
        PainLevel                   INT             NULL,
        SymptomSeverityScore        INT             NULL,
        HasChronicCondition         BIT             NOT NULL CONSTRAINT DF_PatientClinicalFeatures_HasChronic DEFAULT (0),
        HasRecentHospitalization    BIT             NOT NULL CONSTRAINT DF_PatientClinicalFeatures_HasRecentHosp DEFAULT (0),
        PrimarySymptoms             NVARCHAR(1000)  NULL,
        Comorbidities               NVARCHAR(1000)  NULL,
        CapturedAt                  DATETIME2(7)    NOT NULL CONSTRAINT DF_PatientClinicalFeatures_CapturedAt DEFAULT (SYSUTCDATETIME()),
        CapturedByUserId            INT             NULL,
        CONSTRAINT PK_PatientClinicalFeatures PRIMARY KEY CLUSTERED (PatientClinicalFeatureId),
        CONSTRAINT FK_PatientClinicalFeatures_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients (PatientId),
        CONSTRAINT FK_PatientClinicalFeatures_Appointments FOREIGN KEY (AppointmentId) REFERENCES dbo.Appointments (AppointmentId),
        CONSTRAINT FK_PatientClinicalFeatures_Users FOREIGN KEY (CapturedByUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT CK_PatientClinicalFeatures_Age CHECK (Age BETWEEN 0 AND 150),
        CONSTRAINT CK_PatientClinicalFeatures_PainLevel CHECK (PainLevel IS NULL OR PainLevel BETWEEN 0 AND 10),
        CONSTRAINT CK_PatientClinicalFeatures_SymptomSeverity CHECK (SymptomSeverityScore IS NULL OR SymptomSeverityScore BETWEEN 0 AND 10)
    );

    CREATE NONCLUSTERED INDEX IX_PatientClinicalFeatures_PatientId ON dbo.PatientClinicalFeatures (PatientId);
END
GO

-- ML classification results
IF OBJECT_ID(N'dbo.PatientPriorityClassifications', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PatientPriorityClassifications
    (
        PatientPriorityClassificationId INT             IDENTITY(1,1) NOT NULL,
        PatientId                       INT             NOT NULL,
        PatientClinicalFeatureId        INT             NOT NULL,
        MlModelVersionId                INT             NOT NULL,
        PredictedPriorityLevelId        INT             NOT NULL,
        ConfidenceScore                 DECIMAL(5,4)    NOT NULL,
        RiskScore                       DECIMAL(8,4)    NOT NULL,
        ClassificationReason            NVARCHAR(1000)  NULL,
        InputFeaturesJson               NVARCHAR(MAX)   NULL,
        IsCurrent                       BIT             NOT NULL CONSTRAINT DF_PatientPriorityClassifications_IsCurrent DEFAULT (1),
        ClassifiedAt                    DATETIME2(7)    NOT NULL CONSTRAINT DF_PatientPriorityClassifications_ClassifiedAt DEFAULT (SYSUTCDATETIME()),
        ClassifiedByUserId              INT             NULL,
        CONSTRAINT PK_PatientPriorityClassifications PRIMARY KEY CLUSTERED (PatientPriorityClassificationId),
        CONSTRAINT FK_PatientPriorityClassifications_Patients FOREIGN KEY (PatientId) REFERENCES dbo.Patients (PatientId),
        CONSTRAINT FK_PatientPriorityClassifications_ClinicalFeatures FOREIGN KEY (PatientClinicalFeatureId) REFERENCES dbo.PatientClinicalFeatures (PatientClinicalFeatureId),
        CONSTRAINT FK_PatientPriorityClassifications_MlModelVersions FOREIGN KEY (MlModelVersionId) REFERENCES dbo.MlModelVersions (MlModelVersionId),
        CONSTRAINT FK_PatientPriorityClassifications_PriorityLevels FOREIGN KEY (PredictedPriorityLevelId) REFERENCES dbo.PriorityLevels (PriorityLevelId),
        CONSTRAINT FK_PatientPriorityClassifications_Users FOREIGN KEY (ClassifiedByUserId) REFERENCES dbo.Users (UserId),
        CONSTRAINT CK_PatientPriorityClassifications_ConfidenceScore CHECK (ConfidenceScore BETWEEN 0 AND 1),
        CONSTRAINT CK_PatientPriorityClassifications_RiskScore CHECK (RiskScore >= 0)
    );

    CREATE NONCLUSTERED INDEX IX_PatientPriorityClassifications_PatientId_IsCurrent
        ON dbo.PatientPriorityClassifications (PatientId, IsCurrent)
        WHERE IsCurrent = 1;
END
GO

-- Manual override by clinical staff
IF OBJECT_ID(N'dbo.PriorityClassificationOverrides', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PriorityClassificationOverrides
    (
        PriorityClassificationOverrideId    INT             IDENTITY(1,1) NOT NULL,
        PatientPriorityClassificationId     INT             NOT NULL,
        OriginalPriorityLevelId               INT             NOT NULL,
        OverridePriorityLevelId               INT             NOT NULL,
        OverrideReason                        NVARCHAR(500)   NOT NULL,
        OverriddenByUserId                    INT             NOT NULL,
        OverriddenAt                          DATETIME2(7)    NOT NULL CONSTRAINT DF_PriorityClassificationOverrides_OverriddenAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT PK_PriorityClassificationOverrides PRIMARY KEY CLUSTERED (PriorityClassificationOverrideId),
        CONSTRAINT FK_PriorityClassificationOverrides_Classifications FOREIGN KEY (PatientPriorityClassificationId) REFERENCES dbo.PatientPriorityClassifications (PatientPriorityClassificationId),
        CONSTRAINT FK_PriorityClassificationOverrides_OriginalLevel FOREIGN KEY (OriginalPriorityLevelId) REFERENCES dbo.PriorityLevels (PriorityLevelId),
        CONSTRAINT FK_PriorityClassificationOverrides_OverrideLevel FOREIGN KEY (OverridePriorityLevelId) REFERENCES dbo.PriorityLevels (PriorityLevelId),
        CONSTRAINT FK_PriorityClassificationOverrides_Users FOREIGN KEY (OverriddenByUserId) REFERENCES dbo.Users (UserId)
    );
END
GO

-- Link current priority to appointment for queue sorting
IF COL_LENGTH(N'dbo.Appointments', N'CurrentPriorityClassificationId') IS NULL
BEGIN
    ALTER TABLE dbo.Appointments
    ADD CurrentPriorityClassificationId INT NULL;

    ALTER TABLE dbo.Appointments
    ADD CONSTRAINT FK_Appointments_PatientPriorityClassifications
        FOREIGN KEY (CurrentPriorityClassificationId)
        REFERENCES dbo.PatientPriorityClassifications (PatientPriorityClassificationId);
END
GO
