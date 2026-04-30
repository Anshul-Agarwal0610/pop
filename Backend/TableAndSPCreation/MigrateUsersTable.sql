-- ============================================================
-- Migration: Add auth columns to Users table
-- Run this if the Users table was already created by CreateTables.sql
-- ============================================================

USE PollifyDB;
GO

ALTER TABLE Users
    ADD Email        NVARCHAR(255) NULL,
        PasswordHash NVARCHAR(500) NULL,
        AvatarUrl    NVARCHAR(500) NULL,
        AuthProvider NVARCHAR(20)  NOT NULL DEFAULT 'local';
GO

CREATE INDEX IX_Users_Email        ON Users(Email)        WHERE Email IS NOT NULL;
CREATE INDEX IX_Users_AuthProvider ON Users(AuthProvider);
GO
