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
/// Gets the roster for a specific team with optional position and active status filters.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetTeamRosterEndpoint : Endpoint<ListRosterRequest, List<PlayerRecord>>
{
    private readonly IDataGateway _dataGateway;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<GetTeamRosterEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetTeamRosterEndpoint"/> class.
    /// </summary>
    public GetTeamRosterEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider, ILogger<GetTeamRosterEndpoint> logger)
    {
        _dataGateway = dataGateway;
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<GetTeamRosterEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/nfl/teams/{TeamId}/roster");
        Policies("datasets:read");
        Summary(s => s.Summary = "Get team roster");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListRosterRequest req, CancellationToken ct)
    {
        NflLog.GettingTeamRoster(_logger, req.TeamId);

        var dsResult = await _dataSetProvider.Get("NflPlayers", ct);
        if (!dsResult.IsSuccess)
        {
            NflLog.GetTeamRosterFailed(_logger, req.TeamId, "Failed to resolve NflPlayers DataSet");
            AddError("Failed to resolve NflPlayers DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.GetTeamRosterFailed(_logger, req.TeamId, "No active source found for NflPlayers DataSet");
            AddError("No active source found for NflPlayers DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var builder = DataQuery.From<PlayerRecord>(src.DataStoreName, src.PathValue, src.ContainerName)
            .Where("TeamId", req.TeamId);

        if (!string.IsNullOrWhiteSpace(req.Position))
        {
            builder = builder.Where("Position", req.Position);
        }

        if (req.IsActive.HasValue)
        {
            builder = builder.Where("IsActive", req.IsActive.Value);
        }

        var command = builder.OrderBy("LastName").Build();
        var result = await _dataGateway.Execute<IEnumerable<PlayerRecord>>(command, ct);

        if (!result.IsSuccess)
        {
            NflLog.GetTeamRosterFailed(_logger, req.TeamId, result.CurrentMessage!);
            AddError("Failed to get team roster");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var players = result.Value!.ToList();
        if (players.Count == 0)
        {
            NflLog.TeamRosterNotFound(_logger, req.TeamId);
        }
        else
        {
            NflLog.TeamRosterFound(_logger, players.Count, req.TeamId);
        }

        await Send.OkAsync(players, ct);
    }
}
