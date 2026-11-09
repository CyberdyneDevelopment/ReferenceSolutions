using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceNfl.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

namespace FdwReferenceNfl.Endpoints;

/// <summary>
/// Gets all stats for a specific player with optional season filter.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetPlayerStatsEndpoint : Endpoint<PlayerStatsRequest, List<PlayerGameStatRecord>>
{
    private readonly IDataGatewayProvider _dataGateways;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<GetPlayerStatsEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPlayerStatsEndpoint"/> class.
    /// </summary>
    public GetPlayerStatsEndpoint(IDataGatewayProvider dataGateways, DataSetConfigurationProvider dataSetProvider, ILogger<GetPlayerStatsEndpoint> logger)
    {
        _dataGateways = dataGateways;
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<GetPlayerStatsEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/nfl/players/{PlayerId}/stats");
        Policies("datasets:read");
        Summary(s => s.Summary = "Get player stats");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(PlayerStatsRequest req, CancellationToken ct)
    {
        NflLog.GettingPlayerStats(_logger, req.PlayerId);

        var dsResult = await _dataSetProvider.Get("NflPlayerGameStats", ct);
        if (!dsResult.IsSuccess)
        {
            NflLog.GetPlayerStatsFailed(_logger, req.PlayerId, "Failed to resolve NflPlayerGameStats DataSet");
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(dsResult, HttpContext)).ConfigureAwait(false);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.GetPlayerStatsFailed(_logger, req.PlayerId, "No active source found for NflPlayerGameStats DataSet");
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(NflLog.GetPlayerStatsReturnedNothing(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var command = DataQuery.From<PlayerGameStatRecord>(src.DataStoreName, src.PathValue, src.ContainerName)
            .Where("PlayerId", req.PlayerId)
            .Build();

        var result = await _dataGateways.ByName("Main").Execute<IEnumerable<PlayerGameStatRecord>>(command, ct);

        if (!result.IsSuccess)
        {
            NflLog.GetPlayerStatsFailed(_logger, req.PlayerId, result.CurrentMessage!);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
            return;
        }

        var stats = result.Value!.ToList();
        if (stats.Count == 0)
        {
            NflLog.PlayerStatsNotFound(_logger, req.PlayerId);
        }
        else
        {
            NflLog.PlayerStatsFound(_logger, stats.Count, req.PlayerId);
        }

        await Send.OkAsync(stats, ct);
    }
}
