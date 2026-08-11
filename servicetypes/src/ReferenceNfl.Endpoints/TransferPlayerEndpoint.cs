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
/// Transfers a player to a new team.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class TransferPlayerEndpoint : Endpoint<TransferPlayerRequest, PlayerRecord>
{
    private readonly IDataGateway _dataGateway;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<TransferPlayerEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransferPlayerEndpoint"/> class.
    /// </summary>
    public TransferPlayerEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider, ILogger<TransferPlayerEndpoint> logger)
    {
        _dataGateway = dataGateway;
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<TransferPlayerEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/nfl/players/{PlayerId}/transfer");
        Policies("datasets:write");
        Summary(s => s.Summary = "Transfer a player to a new team");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(TransferPlayerRequest req, CancellationToken ct)
    {
        NflLog.TransferringPlayer(_logger, req.PlayerId, req.TeamId);

        var dsResult = await _dataSetProvider.Get("NflPlayers", ct);
        if (!dsResult.IsSuccess)
        {
            NflLog.TransferPlayerFailed(_logger, req.PlayerId, "Failed to resolve NflPlayers DataSet");
            AddError("Failed to resolve NflPlayers DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.TransferPlayerFailed(_logger, req.PlayerId, "No active source found for NflPlayers DataSet");
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
            NflLog.TransferPlayerFailed(_logger, req.PlayerId, getResult.CurrentMessage!);
            AddError("Failed to query player for transfer");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var player = getResult.Value!.FirstOrDefault();
        if (player == null)
        {
            NflLog.TransferPlayerNotFound(_logger, req.PlayerId);
            await Send.NotFoundAsync(ct);
            return;
        }

        var now = DateTimeOffset.UtcNow;

        // Build updated record with new team
        var updated = new PlayerRecord
        {
            Id = player.Id,
            TeamId = req.TeamId,
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
            IsActive = player.IsActive,
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
            NflLog.TransferPlayerFailed(_logger, req.PlayerId, updateResult.CurrentMessage!);
            AddError("Failed to transfer player");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        NflLog.PlayerTransferred(_logger, req.PlayerId, req.TeamId);
        await Send.OkAsync(updated, ct);
    }
}
