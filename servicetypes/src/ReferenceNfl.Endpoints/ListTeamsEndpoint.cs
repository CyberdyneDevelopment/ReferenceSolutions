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
/// Lists all NFL teams with optional conference and division filters.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListTeamsEndpoint : Endpoint<ListTeamsRequest, List<TeamRecord>>
{
    private readonly IDataGateway _dataGateway;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<ListTeamsEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListTeamsEndpoint"/> class.
    /// </summary>
    public ListTeamsEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider, ILogger<ListTeamsEndpoint> logger)
    {
        _dataGateway = dataGateway;
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<ListTeamsEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/nfl/teams");
        Policies("datasets:read");
        Summary(s => s.Summary = "List NFL teams");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListTeamsRequest req, CancellationToken ct)
    {
        NflLog.ListingTeams(_logger);

        var dsResult = await _dataSetProvider.Get("NflTeams", ct);
        if (!dsResult.IsSuccess)
        {
            NflLog.ListTeamsFailed(_logger, "Failed to resolve NflTeams DataSet");
            AddError("Failed to resolve NflTeams DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.ListTeamsFailed(_logger, "No active source found for NflTeams DataSet");
            AddError("No active source found for NflTeams DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var builder = DataQuery.From<TeamRecord>(src.DataStoreName, src.Path, src.ContainerName);

        if (!string.IsNullOrWhiteSpace(req.Conference))
        {
            builder = builder.Where("Conference", req.Conference);
        }

        if (!string.IsNullOrWhiteSpace(req.Division))
        {
            builder = builder.Where("Division", req.Division);
        }

        var command = builder.OrderBy("Name").Build();
        var result = await _dataGateway.Execute<IEnumerable<TeamRecord>>(command, ct);

        if (!result.IsSuccess)
        {
            NflLog.ListTeamsFailed(_logger, result.CurrentMessage!);
            AddError("Failed to list teams");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var teams = result.Value!.ToList();
        NflLog.TeamsFound(_logger, teams.Count);
        await Send.OkAsync(teams, ct);
    }
}
