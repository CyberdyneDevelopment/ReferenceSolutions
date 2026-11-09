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
/// Lists NFL games with optional season, week, and team filters.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListGamesEndpoint : Endpoint<ListGamesRequest, List<GameRecord>>
{
    private readonly IDataGatewayProvider _dataGateways;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<ListGamesEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListGamesEndpoint"/> class.
    /// </summary>
    public ListGamesEndpoint(IDataGatewayProvider dataGateways, DataSetConfigurationProvider dataSetProvider, ILogger<ListGamesEndpoint> logger)
    {
        _dataGateways = dataGateways;
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<ListGamesEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/nfl/games");
        Policies("datasets:read");
        Summary(s => s.Summary = "List NFL games");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListGamesRequest req, CancellationToken ct)
    {
        NflLog.ListingGames(_logger);

        var dsResult = await _dataSetProvider.Get("NflGames", ct);
        if (!dsResult.IsSuccess)
        {
            NflLog.ListGamesFailed(_logger, "Failed to resolve NflGames DataSet");
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(dsResult, HttpContext)).ConfigureAwait(false);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.ListGamesFailed(_logger, "No active source found for NflGames DataSet");
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(NflLog.ListGamesReturnedNothing(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var builder = DataQuery.From<GameRecord>(src.DataStoreName, src.PathValue, src.ContainerName);

        if (req.Season.HasValue)
        {
            builder = builder.Where("Season", req.Season.Value);
        }

        if (req.Week.HasValue)
        {
            builder = builder.Where("Week", req.Week.Value);
        }

        if (req.TeamId.HasValue)
        {
            builder = builder.BeginOrGroup()
                .Where("HomeTeamId", req.TeamId.Value)
                .Where("AwayTeamId", req.TeamId.Value)
                .EndGroup();
        }

        var command = builder.OrderBy("GameDate").Build();
        var result = await _dataGateways.ByName("Main").Execute<IEnumerable<GameRecord>>(command, ct);

        if (!result.IsSuccess)
        {
            NflLog.ListGamesFailed(_logger, result.CurrentMessage!);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
            return;
        }

        var games = result.Value!.ToList();
        NflLog.GamesFound(_logger, games.Count);
        await Send.OkAsync(games, ct);
    }
}
