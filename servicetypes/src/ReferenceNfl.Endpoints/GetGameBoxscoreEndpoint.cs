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
using ReferenceNfl.Endpoints.Logging;

namespace ReferenceNfl.Endpoints;

/// <summary>
/// Gets the boxscore (player stats) for a specific game.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetGameBoxscoreEndpoint : Endpoint<GameIdRequest, List<PlayerGameStatRecord>>
{
    private readonly IDataGateway _dataGateway;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<GetGameBoxscoreEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetGameBoxscoreEndpoint"/> class.
    /// </summary>
    public GetGameBoxscoreEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider, ILogger<GetGameBoxscoreEndpoint> logger)
    {
        _dataGateway = dataGateway;
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<GetGameBoxscoreEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/nfl/games/{GameId}/boxscore");
        Policies("datasets:read");
        Summary(s => s.Summary = "Get game boxscore");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(GameIdRequest req, CancellationToken ct)
    {
        NflLog.GettingGameBoxscore(_logger, req.GameId);

        var dsResult = await _dataSetProvider.Get("NflPlayerGameStats", ct);
        if (!dsResult.IsSuccess)
        {
            NflLog.GetGameBoxscoreFailed(_logger, req.GameId, "Failed to resolve NflPlayerGameStats DataSet");
            AddError("Failed to resolve NflPlayerGameStats DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.GetGameBoxscoreFailed(_logger, req.GameId, "No active source found for NflPlayerGameStats DataSet");
            AddError("No active source found for NflPlayerGameStats DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var command = DataQuery.From<PlayerGameStatRecord>(src.DataStoreName, src.PathValue, src.ContainerName)
            .Where("GameId", req.GameId)
            .Build();

        var result = await _dataGateway.Execute<IEnumerable<PlayerGameStatRecord>>(command, ct);

        if (!result.IsSuccess)
        {
            NflLog.GetGameBoxscoreFailed(_logger, req.GameId, result.CurrentMessage!);
            AddError("Failed to get game boxscore");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var boxscore = result.Value!.ToList();
        if (boxscore.Count == 0)
        {
            NflLog.GameBoxscoreNotFound(_logger, req.GameId);
        }
        else
        {
            NflLog.GameBoxscoreFound(_logger, boxscore.Count, req.GameId);
        }

        await Send.OkAsync(boxscore, ct);
    }
}
