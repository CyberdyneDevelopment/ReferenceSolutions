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
/// Creates a new player record.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreatePlayerEndpoint : Endpoint<CreatePlayerRequest, PlayerRecord>
{
    private readonly IDataGateway _dataGateway;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<CreatePlayerEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePlayerEndpoint"/> class.
    /// </summary>
    public CreatePlayerEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider, ILogger<CreatePlayerEndpoint> logger)
    {
        _dataGateway = dataGateway;
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<CreatePlayerEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/nfl/players");
        Policies("datasets:write");
        Summary(s => s.Summary = "Create a new player");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CreatePlayerRequest req, CancellationToken ct)
    {
        NflLog.CreatingPlayer(_logger, req.FirstName, req.LastName, req.TeamId);

        var dsResult = await _dataSetProvider.Get("NflPlayers", ct);
        if (!dsResult.IsSuccess)
        {
            NflLog.CreatePlayerFailed(_logger, req.FirstName, req.LastName, "Failed to resolve NflPlayers DataSet");
            AddError("Failed to resolve NflPlayers DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.CreatePlayerFailed(_logger, req.FirstName, req.LastName, "No active source found for NflPlayers DataSet");
            AddError("No active source found for NflPlayers DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var record = new PlayerRecord
        {
            Id = Guid.NewGuid(),
            TeamId = req.TeamId,
            FirstName = req.FirstName,
            LastName = req.LastName,
            Position = req.Position,
            JerseyNumber = req.JerseyNumber,
            HeightInches = req.HeightInches,
            WeightLbs = req.WeightLbs,
            College = req.College,
            DraftYear = req.DraftYear,
            DraftRound = req.DraftRound,
            DraftPick = req.DraftPick,
            IsActive = true,
            CreateDate = now,
            CreateBy = "api",
            CreateOnBehalfOf = string.Empty,
            ModifyDate = now,
            ModifyBy = "api",
            ModifyOnBehalfOf = string.Empty
        };

        var command = Insert.Into<PlayerRecord>(src.ContainerName)
            .DataStore(src.DataStoreName).Path(src.Path).Value(record);

        var result = await _dataGateway.Execute<int>(command, ct);

        if (!result.IsSuccess)
        {
            NflLog.CreatePlayerFailed(_logger, req.FirstName, req.LastName, result.CurrentMessage!);
            AddError("Failed to create player");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        NflLog.PlayerCreated(_logger, record.Id, record.FirstName, record.LastName);
        await Send.CreatedAtAsync<GetTeamRosterEndpoint>(
            new { TeamId = record.TeamId },
            record,
            cancellation: ct);
    }
}
