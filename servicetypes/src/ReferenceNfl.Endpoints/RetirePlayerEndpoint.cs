using System.Diagnostics.CodeAnalysis;
using System;
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
/// Retires a player by setting IsActive to false.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RetirePlayerEndpoint : Endpoint<PlayerIdRequest, PlayerRecord>
{
    private readonly IDataGateway _dataGateway;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<RetirePlayerEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RetirePlayerEndpoint"/> class.
    /// </summary>
    public RetirePlayerEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider, ILogger<RetirePlayerEndpoint> logger)
    {
        _dataGateway = dataGateway;
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<RetirePlayerEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/nfl/players/{PlayerId}/retire");
        Policies("datasets:write");
        Summary(s => s.Summary = "Retire a player");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(PlayerIdRequest req, CancellationToken ct)
    {
        NflLog.RetiringPlayer(_logger, req.PlayerId);

        var dsResult = await _dataSetProvider.Get("NflPlayers", ct);
        if (!dsResult.IsSuccess)
        {
            NflLog.RetirePlayerFailed(_logger, req.PlayerId, "Failed to resolve NflPlayers DataSet");
            AddError("Failed to resolve NflPlayers DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.RetirePlayerFailed(_logger, req.PlayerId, "No active source found for NflPlayers DataSet");
            AddError("No active source found for NflPlayers DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        // Query existing player
        var getCommand = DataQuery.From<PlayerRecord>(src.DataStoreName, src.Path, src.ContainerName)
            .Where("Id", req.PlayerId)
            .Build();

        var getResult = await _dataGateway.Execute<IEnumerable<PlayerRecord>>(getCommand, ct);

        if (!getResult.IsSuccess)
        {
            NflLog.RetirePlayerFailed(_logger, req.PlayerId, getResult.CurrentMessage!);
            AddError("Failed to query player for retirement");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var player = getResult.Value!.FirstOrDefault();
        if (player == null)
        {
            NflLog.RetirePlayerNotFound(_logger, req.PlayerId);
            await Send.NotFoundAsync(ct);
            return;
        }

        var now = DateTimeOffset.UtcNow;

        // Build updated record with IsActive = false
        var updated = new PlayerRecord
        {
            Id = player.Id,
            TeamId = player.TeamId,
            FirstName = player.FirstName,
            LastName = player.LastName,
            Position = player.Position,
            JerseyNumber = player.JerseyNumber,
            HeightInches = player.HeightInches,
            WeightLbs = player.WeightLbs,
            College = player.College,
            DraftYear = player.DraftYear,
            DraftRound = player.DraftRound,
            DraftPick = player.DraftPick,
            IsActive = false,
            CreateDate = player.CreateDate,
            CreateBy = player.CreateBy,
            CreateOnBehalfOf = player.CreateOnBehalfOf,
            ModifyDate = now,
            ModifyBy = "api",
            ModifyOnBehalfOf = string.Empty
        };

        var updateCommand = new UpdateCommandBuilder<PlayerRecord>(src.ContainerName)
            .DataStore(src.DataStoreName)
            .Path(src.Path)
            .Where("Id", req.PlayerId)
            .Value(updated);

        var updateResult = await _dataGateway.Execute<int>(updateCommand, ct);

        if (!updateResult.IsSuccess)
        {
            NflLog.RetirePlayerFailed(_logger, req.PlayerId, updateResult.CurrentMessage!);
            AddError("Failed to retire player");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        NflLog.PlayerRetired(_logger, req.PlayerId);
        await Send.OkAsync(updated, ct);
    }
}
