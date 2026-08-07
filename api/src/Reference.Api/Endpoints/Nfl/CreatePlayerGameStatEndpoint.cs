using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data;
using Fdw.Commands.Data.Extensions;
using Fdw.Data.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Api.Logging;
using Reference.Nfl;

namespace Reference.Api.Endpoints;

/// <summary>
/// Creates a player game stat record for a specific game.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreatePlayerGameStatEndpoint : Endpoint<CreateStatRequest, PlayerGameStatRecord>
{
    private readonly IDataGateway _dataGateway;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<CreatePlayerGameStatEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePlayerGameStatEndpoint"/> class.
    /// </summary>
    public CreatePlayerGameStatEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider, ILogger<CreatePlayerGameStatEndpoint> logger)
    {
        _dataGateway = dataGateway;
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<CreatePlayerGameStatEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/nfl/games/{GameId}/stats");
        Policies("datasets:write");
        Summary(s => s.Summary = "Create a player game stat");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CreateStatRequest req, CancellationToken ct)
    {
        NflLog.CreatingPlayerGameStat(_logger, req.PlayerId, req.GameId);

        var dsResult = await _dataSetProvider.Get("NflPlayerGameStats", ct);
        if (!dsResult.IsSuccess)
        {
            NflLog.CreatePlayerGameStatFailed(_logger, req.PlayerId, req.GameId, "Failed to resolve NflPlayerGameStats DataSet");
            AddError("Failed to resolve NflPlayerGameStats DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.CreatePlayerGameStatFailed(_logger, req.PlayerId, req.GameId, "No active source found for NflPlayerGameStats DataSet");
            AddError("No active source found for NflPlayerGameStats DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var record = new PlayerGameStatRecord
        {
            Id = Guid.NewGuid(),
            GameId = req.GameId,
            PlayerId = req.PlayerId,
            PassingYards = req.PassingYards,
            PassingTDs = req.PassingTDs,
            Interceptions = req.Interceptions,
            RushingYards = req.RushingYards,
            RushingTDs = req.RushingTDs,
            Receptions = req.Receptions,
            ReceivingYards = req.ReceivingYards,
            ReceivingTDs = req.ReceivingTDs,
            Tackles = req.Tackles,
            Sacks = req.Sacks,
            FumblesForced = req.FumblesForced,
            CreateDate = now,
            CreateBy = "api",
            CreateOnBehalfOf = string.Empty,
            ModifyDate = now,
            ModifyBy = "api",
            ModifyOnBehalfOf = string.Empty
        };

        var command = Insert.Into<PlayerGameStatRecord>(src.ContainerName)
            .DataStore(src.DataStoreName).Path(src.Path).Value(record);

        var result = await _dataGateway.Execute<int>(command, ct);

        if (!result.IsSuccess)
        {
            NflLog.CreatePlayerGameStatFailed(_logger, req.PlayerId, req.GameId, result.CurrentMessage!);
            AddError("Failed to create player game stat");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        NflLog.PlayerGameStatCreated(_logger, record.Id, record.PlayerId, record.GameId);
        await Send.CreatedAtAsync<GetGameBoxscoreEndpoint>(
            new { GameId = record.GameId },
            record,
            cancellation: ct);
    }
}
