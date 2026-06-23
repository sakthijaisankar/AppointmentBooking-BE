-- =============================================
-- Script: Insert/002_Seed_PriorityLevels.sql
-- =============================================

USE [AppointmentBooking];
GO

IF NOT EXISTS (SELECT 1 FROM dbo.PriorityLevels WHERE LevelCode = N'CRITICAL')
    INSERT INTO dbo.PriorityLevels (LevelCode, LevelName, SortOrder, ColorHex, Description)
    VALUES (N'CRITICAL', N'Critical', 1, N'#DC2626', N'Immediate attention required - life-threatening');

IF NOT EXISTS (SELECT 1 FROM dbo.PriorityLevels WHERE LevelCode = N'HIGH')
    INSERT INTO dbo.PriorityLevels (LevelCode, LevelName, SortOrder, ColorHex, Description)
    VALUES (N'HIGH', N'High', 2, N'#EA580C', N'Urgent care needed within hours');

IF NOT EXISTS (SELECT 1 FROM dbo.PriorityLevels WHERE LevelCode = N'MEDIUM')
    INSERT INTO dbo.PriorityLevels (LevelCode, LevelName, SortOrder, ColorHex, Description)
    VALUES (N'MEDIUM', N'Medium', 3, N'#CA8A04', N'Routine priority with moderate symptoms');

IF NOT EXISTS (SELECT 1 FROM dbo.PriorityLevels WHERE LevelCode = N'LOW')
    INSERT INTO dbo.PriorityLevels (LevelCode, LevelName, SortOrder, ColorHex, Description)
    VALUES (N'LOW', N'Low', 4, N'#16A34A', N'Non-urgent, standard scheduling');
GO
