-- =============================================
-- Script: 007_Create_PasswordResetTokens.sql
-- Module: Authentication & Authorization
-- Dependencies: Users
-- =============================================

USE [AppointmentBooking];
GO

IF COL_LENGTH(N'dbo.Users', N'PhoneNumber') IS NULL
BEGIN
    ALTER TABLE dbo.Users ADD PhoneNumber NVARCHAR(20) NULL;
END
GO

IF OBJECT_ID(N'dbo.PasswordResetTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PasswordResetTokens
    (
        PasswordResetTokenId  INT             IDENTITY(1,1) NOT NULL,
        UserId                INT             NOT NULL,
        Token                 NVARCHAR(500)   NOT NULL,
        ExpiresAt             DATETIME2(7)    NOT NULL,
        IsUsed                BIT             NOT NULL CONSTRAINT DF_PasswordResetTokens_IsUsed DEFAULT (0),
        CreatedAt             DATETIME2(7)    NOT NULL CONSTRAINT DF_PasswordResetTokens_CreatedAt DEFAULT (SYSUTCDATETIME()),
        UsedAt                DATETIME2(7)    NULL,
        CONSTRAINT PK_PasswordResetTokens PRIMARY KEY CLUSTERED (PasswordResetTokenId),
        CONSTRAINT UQ_PasswordResetTokens_Token UNIQUE (Token),
        CONSTRAINT FK_PasswordResetTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users (UserId) ON DELETE CASCADE
    );

    CREATE NONCLUSTERED INDEX IX_PasswordResetTokens_UserId_IsUsed
        ON dbo.PasswordResetTokens (UserId, IsUsed)
        WHERE IsUsed = 0;
END
GO
