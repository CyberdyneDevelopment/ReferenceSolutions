using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Reference.Scheduler.Server.Logging;

namespace Reference.Scheduler.Server.Services;

/// <summary>
/// Repository for accessing calculation usage and pre-compute schedule data via IDataGateway.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CalculationUsageRepository : ICalculationUsageRepository
{
    private const string DataStoreName = "ConfigurationDb";
    private const string PathName = "calc";
    private const string CalculationUsageContainer = "CalculationUsage";
    private const string PreComputeScheduleContainer = "PreComputeSchedule";

    private readonly IDataGateway _dataGateway;
    private readonly ILogger<CalculationUsageRepository> _logger;

    public CalculationUsageRepository(
        IDataGateway dataGateway,
        ILogger<CalculationUsageRepository> logger)
    {
        _dataGateway = dataGateway;
        _logger = logger;
    }

    public async Task<IGenericResult<IReadOnlyList<CalculationUsageRecord>>> GetStaleCalculations(
        int minExecutionCount,
        int stalenessThresholdMinutes,
        int maxResults,
        CancellationToken cancellationToken)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-stalenessThresholdMinutes);

            var query = Query.From<CalculationUsageRecord>(DataStoreName, PathName, CalculationUsageContainer)
                .Where(r => r.ExecutionCount).GreaterThanOrEqual(minExecutionCount)
                .BeginOrGroup()
                    .Where("LastCachedAt", new Fdw.Data.IsNullOperator(), null)
                    .Where("LastCachedAt", new Fdw.Data.LessThanOperator(), cutoffTime)
                .EndGroup()
                .OrderByDescending(r => r.ExecutionCount)
                .Paging(0, maxResults)
                .Build();

            var result = await _dataGateway.Execute<IEnumerable<CalculationUsageRecord>>(query, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                PreComputeLog.FailedToGetStaleCalculations(_logger, PreComputeLog.GetError(result, _logger));
                return GenericResult<IReadOnlyList<CalculationUsageRecord>>.Failure(result.Messages.ToArray());
            }

            var records = result.Value!.ToList();
            return GenericResult<IReadOnlyList<CalculationUsageRecord>>.Success(records);
        }
        catch (Exception ex)
        {
            return GenericResult<IReadOnlyList<CalculationUsageRecord>>.Failure(
                PreComputeLog.FailedToGetStaleCalculations(_logger, ex.Message));
        }
    }

    public async Task<IGenericResult> UpdateLastCachedAt(string calculationHash, CancellationToken cancellationToken)
    {
        try
        {
            var record = new CalculationUsageCacheUpdate
            {
                LastCachedAt = DateTime.UtcNow
            };

            var command = Update.In<CalculationUsageCacheUpdate>(CalculationUsageContainer)
                .DataStore(DataStoreName)
                .Path(PathName)
                .Where("CalculationHash", calculationHash)
                .Value(record);

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return GenericResult.Failure(
                    PreComputeLog.PreComputeFailed(_logger, calculationHash, PreComputeLog.GetError(result, _logger)));
            }

            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                PreComputeLog.PreComputeFailed(_logger, calculationHash, ex.Message));
        }
    }

    public async Task<IGenericResult> RecordPreComputeRun(
        int successCount,
        int failureCount,
        int calculationsProcessed,
        long durationMs,
        CancellationToken cancellationToken)
    {
        try
        {
            var record = new PreComputeScheduleRecord
            {
                LastPreComputedAt = DateTime.UtcNow,
                SuccessCount = successCount,
                FailureCount = failureCount,
                CalculationsProcessed = calculationsProcessed,
                DurationMs = durationMs
            };

            var command = Insert.Into<PreComputeScheduleRecord>(PreComputeScheduleContainer)
                .DataStore(DataStoreName)
                .Path(PathName)
                .Value(record);

            var result = await _dataGateway.Execute<int>(command, cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess)
            {
                return GenericResult.Failure(
                    PreComputeLog.PreComputeFailed(_logger, "PreComputeSchedule", PreComputeLog.GetError(result, _logger)));
            }

            return GenericResult.Success();
        }
        catch (Exception ex)
        {
            return GenericResult.Failure(
                PreComputeLog.PreComputeFailed(_logger, "PreComputeSchedule", ex.Message));
        }
    }
}

/// <summary>
/// Record for updating the LastCachedAt field on CalculationUsage.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class CalculationUsageCacheUpdate
{
    public DateTime LastCachedAt { get; set; }
}

/// <summary>
/// Record for inserting a pre-compute run into PreComputeSchedule.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class PreComputeScheduleRecord
{
    public DateTime LastPreComputedAt { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int CalculationsProcessed { get; set; }
    public long DurationMs { get; set; }
}
