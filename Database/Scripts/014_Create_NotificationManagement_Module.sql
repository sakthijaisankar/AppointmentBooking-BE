USE [AppointmentBooking];
GO

-- CREATE NOTIFICATION MANAGEMENT MODULE TABLES

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'NotificationTemplates')
BEGIN
    CREATE TABLE [dbo].[NotificationTemplates] (
        [TemplateId] INT IDENTITY(1,1) NOT NULL,
        [TemplateCode] NVARCHAR(50) NOT NULL,
        [TemplateName] NVARCHAR(100) NOT NULL,
        [SubjectTemplate] NVARCHAR(200) NULL,
        [BodyTemplate] NVARCHAR(MAX) NOT NULL,
        [DefaultChannel] NVARCHAR(20) NOT NULL, -- Email, SMS, Push, All
        [IsActive] BIT NOT NULL CONSTRAINT [DF_NotificationTemplates_IsActive] DEFAULT (1),
        [CreatedAt] DATETIME NOT NULL CONSTRAINT [DF_NotificationTemplates_CreatedAt] DEFAULT (GETUTCDATE()),
        [UpdatedAt] DATETIME NULL,
        CONSTRAINT [PK_NotificationTemplates] PRIMARY KEY CLUSTERED ([TemplateId] ASC),
        CONSTRAINT [UQ_NotificationTemplates_TemplateCode] UNIQUE ([TemplateCode])
    );
END;

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Notifications')
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [NotificationId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [AppointmentId] INT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Body] NVARCHAR(MAX) NOT NULL,
        [Channel] NVARCHAR(20) NOT NULL, -- Email, SMS, Push
        [Status] NVARCHAR(20) NOT NULL, -- Pending, Sent, Failed
        [ErrorMessage] NVARCHAR(MAX) NULL,
        [IsRead] BIT NOT NULL CONSTRAINT [DF_Notifications_IsRead] DEFAULT (0),
        [SentAt] DATETIME NULL,
        [CreatedAt] DATETIME NOT NULL CONSTRAINT [DF_Notifications_CreatedAt] DEFAULT (GETUTCDATE()),
        CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([NotificationId] ASC),
        CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId]) ON DELETE CASCADE,
        CONSTRAINT [FK_Notifications_Appointments] FOREIGN KEY ([AppointmentId]) REFERENCES [dbo].[Appointments] ([AppointmentId]) ON DELETE SET NULL
    );
END;

-- SEED MOCK TEMPLATES IF THEY DO NOT EXIST
IF NOT EXISTS (SELECT * FROM [dbo].[NotificationTemplates] WHERE [TemplateCode] = 'APPOINTMENT_BOOKED')
BEGIN
    INSERT INTO [dbo].[NotificationTemplates] ([TemplateCode], [TemplateName], [SubjectTemplate], [BodyTemplate], [DefaultChannel])
    VALUES ('APPOINTMENT_BOOKED', 'Appointment Booked Confirmation', 'Appointment Booked: {AppointmentNumber}', 'Dear {PatientName}, your appointment #{AppointmentNumber} with Dr. {DoctorName} has been booked for {ScheduledTime}.', 'All');
END;

IF NOT EXISTS (SELECT * FROM [dbo].[NotificationTemplates] WHERE [TemplateCode] = 'APPOINTMENT_CONFIRMED')
BEGIN
    INSERT INTO [dbo].[NotificationTemplates] ([TemplateCode], [TemplateName], [SubjectTemplate], [BodyTemplate], [DefaultChannel])
    VALUES ('APPOINTMENT_CONFIRMED', 'Appointment Confirmed Status Update', 'Appointment Confirmed: {AppointmentNumber}', 'Dear {PatientName}, your appointment #{AppointmentNumber} with Dr. {DoctorName} on {ScheduledTime} has been confirmed.', 'All');
END;

IF NOT EXISTS (SELECT * FROM [dbo].[NotificationTemplates] WHERE [TemplateCode] = 'APPOINTMENT_CANCELLED')
BEGIN
    INSERT INTO [dbo].[NotificationTemplates] ([TemplateCode], [TemplateName], [SubjectTemplate], [BodyTemplate], [DefaultChannel])
    VALUES ('APPOINTMENT_CANCELLED', 'Appointment Cancelled Status Update', 'Appointment Cancelled: {AppointmentNumber}', 'Dear {PatientName}, your appointment #{AppointmentNumber} with Dr. {DoctorName} on {ScheduledTime} has been cancelled. Reason: {Reason}', 'All');
END;

IF NOT EXISTS (SELECT * FROM [dbo].[NotificationTemplates] WHERE [TemplateCode] = 'QUEUE_CALLING')
BEGIN
    INSERT INTO [dbo].[NotificationTemplates] ([TemplateCode], [TemplateName], [SubjectTemplate], [BodyTemplate], [DefaultChannel])
    VALUES ('QUEUE_CALLING', 'Practitioner Calling Patient Alert', 'Consultation Calling: Ticket {QueueNumber}', 'Dear {PatientName}, Dr. {DoctorName} is now ready to see you. Please proceed to consultation room. Ticket number: {QueueNumber}.', 'All');
END;

IF NOT EXISTS (SELECT * FROM [dbo].[NotificationTemplates] WHERE [TemplateCode] = 'CONSULTATION_COMPLETED')
BEGIN
    INSERT INTO [dbo].[NotificationTemplates] ([TemplateCode], [TemplateName], [SubjectTemplate], [BodyTemplate], [DefaultChannel])
    VALUES ('CONSULTATION_COMPLETED', 'Consultation Completed & Prescription Ready', 'Prescription Slip Ready: {AppointmentNumber}', 'Dear {PatientName}, your consultation with Dr. {DoctorName} is complete. Your diagnosis is: {Diagnosis}. Your prescription slip is ready and can be viewed inside your Healwell account.', 'All');
END;
