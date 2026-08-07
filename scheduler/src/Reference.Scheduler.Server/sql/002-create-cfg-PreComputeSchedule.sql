-- cfg.PreComputeSchedule
-- Tracks pre-compute job run history for monitoring and diagnostics.
-- Each row represents a single pre-compute run with success/failure counts.

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'cfg' AND t.name = 'PreComputeSchedule')
BEGIN
    CREATE TABLE [cfg].[PreComputeSchedule]
    (
        [RowId]                     UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        [LastPreComputedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
        [SuccessCount]              INT                 NOT NULL DEFAULT 0,
        [FailureCount]              INT                 NOT NULL DEFAULT 0,
        [CalculationsProcessed]     INT                 NOT NULL DEFAULT 0,
        [DurationMs]                BIGINT              NOT NULL DEFAULT 0,
        [IsCurrent]                 BIT                 NOT NULL DEFAULT 1,
        [IsDeleted]                 BIT                 NOT NULL DEFAULT 0,

        CONSTRAINT [PK_PreComputeSchedule] PRIMARY KEY ([RowId])
    );

    CREATE INDEX [IX_PreComputeSchedule_LastRun]
        ON [cfg].[PreComputeSchedule]([LastPreComputedAt] DESC)
        WHERE [IsCurrent] = 1 AND [IsDeleted] = 0;
END
GO
