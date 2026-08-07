-- E2E Test Supplemental Seed Data
-- Fills gaps not covered by the main seed scripts (docker/mssql/seed/).
-- Run AFTER the main seed-data.sql. Idempotent (IF NOT EXISTS guards).

SET QUOTED_IDENTIFIER ON;
GO

USE ControlDb;
GO

-- ============================================================================
-- Theme: DefaultTheme (light mode, marked as default)
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM cfg.Theme WHERE Name = 'Default' AND IsCurrent = 1 AND IsDeleted = 0)
BEGIN
    INSERT INTO cfg.Theme (Id, Name, DisplayName, Description, IsDefault, IsDarkMode,
        PrimaryColor, SecondaryColor, BackgroundColor, SurfaceColor,
        TextPrimary, TextSecondary)
    VALUES (
        'E2E00001-0000-0000-0000-000000000001',
        'Default',
        'Default Light',
        'Default built-in theme for the reference solution',
        1, -- IsDefault
        0, -- IsDarkMode (light mode)
        '#1976D2',   -- PrimaryColor (Material Blue)
        '#424242',   -- SecondaryColor (Dark Grey)
        '#FFFFFF',   -- BackgroundColor (White)
        '#FAFAFA',   -- SurfaceColor (Light Grey)
        '#212121',   -- TextPrimary (Near Black)
        '#757575'    -- TextSecondary (Medium Grey)
    );
    PRINT 'Seeded Theme: Default (light)';
END
GO

-- ============================================================================
-- Theme: DarkTheme
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM cfg.Theme WHERE Name = 'Dark' AND IsCurrent = 1 AND IsDeleted = 0)
BEGIN
    INSERT INTO cfg.Theme (Id, Name, DisplayName, Description, IsDefault, IsDarkMode,
        PrimaryColor, SecondaryColor, BackgroundColor, SurfaceColor,
        TextPrimary, TextSecondary)
    VALUES (
        'E2E00001-0000-0000-0000-000000000002',
        'Dark',
        'Dark Mode',
        'Dark mode theme',
        0, -- IsDefault
        1, -- IsDarkMode
        '#90CAF9',   -- PrimaryColor (Light Blue)
        '#616161',   -- SecondaryColor (Grey)
        '#121212',   -- BackgroundColor (Dark)
        '#1E1E1E',   -- SurfaceColor (Dark Surface)
        '#E0E0E0',   -- TextPrimary (Light Grey)
        '#9E9E9E'    -- TextSecondary (Medium Grey)
    );
    PRINT 'Seeded Theme: Dark';
END
GO

-- ============================================================================
-- DataFlow edges: DataArchiveCopy pipeline connects Schedules -> PipelineExecutions
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM cfg.DataFlow WHERE Name = 'SchedulesToExecutions')
BEGIN
    DECLARE @SchedulesId UNIQUEIDENTIFIER;
    DECLARE @PipelineExecId UNIQUEIDENTIFIER;

    SELECT @SchedulesId = Id FROM cfg.DataSet WHERE Name = 'Schedules';
    SELECT @PipelineExecId = Id FROM cfg.DataSet WHERE Name = 'PipelineExecutions';

    IF @SchedulesId IS NOT NULL AND @PipelineExecId IS NOT NULL
    BEGIN
        INSERT INTO cfg.DataFlow (Id, Name, SourceDataSetId, TargetDataSetId, PipelineName, FlowType, Description)
        VALUES (
            'E2E00002-0000-0000-0000-000000000001',
            'SchedulesToExecutions',
            @SchedulesId,
            @PipelineExecId,
            'DataArchiveCopy',
            'Pipeline',
            'DataArchiveCopy pipeline moves schedule configs to execution records'
        );
        PRINT 'Seeded DataFlow: SchedulesToExecutions';
    END
    ELSE
    BEGIN
        PRINT 'WARNING: DataSet Schedules or PipelineExecutions not found for DataFlow seed';
    END
END
GO

PRINT 'E2E supplemental seed data complete.';
GO
