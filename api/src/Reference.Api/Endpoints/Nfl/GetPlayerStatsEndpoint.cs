using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data;
using Fdw.Data.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Api.Logging;
using Reference.Nfl;

namespace Reference.Api.Endpoints;

/// <summary>
/// Gets all stats for a specific player with optional season filter.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetPlayerStatsEndpoint : Endpoint<PlayerStatsRequest, List<PlayerGameStatRecord>>
{
    private readonly IDataGateway _dataGateway;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<GetPlayerStatsEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetPlayerStatsEndpoint"/> class.
    /// </summary>
    public GetPlayerStatsEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider, ILogger<GetPlayerStatsEndpoint> logger)
    {
        _dataGateway = dataGateway;
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
            AddError("Failed to resolve NflPlayerGameStats DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.GetPlayerStatsFailed(_logger, req.PlayerId, "No active source found for NflPlayerGameStats DataSet");
            AddError("No active source found for NflPlayerGameStats DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var command = DataQuery.From<PlayerGameStatRecord>(src.DataStoreName, src.Path, src.ContainerName)
            .Where("PlayerId", req.PlayerId)
            .Build();

        var result = await _dataGateway.Execute<IEnumerable<PlayerGameStatRecord>>(command, ct);

        if (!result.IsSuccess)
        {
            NflLog.GetPlayerStatsFailed(_logger, req.PlayerId, result.CurrentMessage!);
            AddError("Failed to get player stats");
            await Send.ErrorsAsync(500, ct);
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
