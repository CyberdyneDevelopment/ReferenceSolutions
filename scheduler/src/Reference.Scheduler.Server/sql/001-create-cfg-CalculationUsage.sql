-- cfg.CalculationUsage
-- Tracks calculation execution frequency and staleness for pre-compute scheduling.
-- Used by PreComputeCalculationsJob to identify high-value calculations worth pre-computing.

IF NOT EXISTS (SELECT 1 FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id WHERE s.name = 'cfg' AND t.name = 'CalculationUsage')
BEGIN
    CREATE TABLE [cfg].[CalculationUsage]
    (
        [RowId]             UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWSEQUENTIALID(),
        [CalculationType]   VARCHAR(200)        NOT NULL,
        [CalculationHash]   VARCHAR(64)         NOT NULL,
        [ExecutionCount]    INT                 NOT NULL DEFAULT 0,
        [LastExecutedAt]    DATETIME2           NULL,
        [AverageDurationMs] BIGINT              NOT NULL DEFAULT 0,
        [LastCachedAt]      DATETIME2           NULL,
        [SuccessCount]      INT                 NOT NULL DEFAULT 0,
        [FailureCount]      INT                 NOT NULL DEFAULT 0,
        [IsCurrent]         BIT                 NOT NULL DEFAULT 1,
        [IsDeleted]         BIT                 NOT NULL DEFAULT 0,

        CONSTRAINT [PK_CalculationUsage] PRIMARY KEY ([RowId])
    );

    CREATE UNIQUE INDEX [UX_CalculationUsage_Hash_Current]
        ON [cfg].[CalculationUsage]([CalculationHash])
        WHERE [IsCurrent] = 1 AND [IsDeleted] = 0;

    CREATE INDEX [IX_CalculationUsage_Staleness]
        ON [cfg].[CalculationUsage]([ExecutionCount], [LastCachedAt])
        WHERE [IsCurrent] = 1 AND [IsDeleted] = 0;
END
GO
