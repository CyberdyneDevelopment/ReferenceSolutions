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
/// Lists season standings with optional season and conference filters.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListStandingsEndpoint : Endpoint<ListStandingsRequest, List<SeasonStandingRecord>>
{
    private readonly IDataGatewayProvider _dataGateways;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<ListStandingsEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListStandingsEndpoint"/> class.
    /// </summary>
    public ListStandingsEndpoint(IDataGatewayProvider dataGateways, DataSetConfigurationProvider dataSetProvider, ILogger<ListStandingsEndpoint> logger)
    {
        _dataGateways = dataGateways;
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<ListStandingsEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/nfl/standings");
        Policies("datasets:read");
        Summary(s => s.Summary = "List season standings");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(ListStandingsRequest req, CancellationToken ct)
    {
        NflLog.ListingStandings(_logger);

        var dsResult = await _dataSetProvider.Get("NflSeasonStandings", ct);
        if (!dsResult.IsSuccess)
        {
            NflLog.ListStandingsFailed(_logger, "Failed to resolve NflSeasonStandings DataSet");
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(dsResult, HttpContext)).ConfigureAwait(false);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.ListStandingsFailed(_logger, "No active source found for NflSeasonStandings DataSet");
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(NflLog.ListStandingsReturnedNothing(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var builder = DataQuery.From<SeasonStandingRecord>(src.DataStoreName, src.PathValue, src.ContainerName);

        if (req.Season.HasValue)
        {
            builder = builder.Where("Season", req.Season.Value);
        }

        if (!string.IsNullOrWhiteSpace(req.Conference))
        {
            builder = builder.Where("Conference", req.Conference);
        }

        var command = builder.OrderByDescending("Wins").Build();
        var result = await _dataGateways.ByName("Main").Execute<IEnumerable<SeasonStandingRecord>>(command, ct);

        if (!result.IsSuccess)
        {
            NflLog.ListStandingsFailed(_logger, result.CurrentMessage!);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
            return;
        }

        var standings = result.Value!.ToList();
        NflLog.StandingsFound(_logger, standings.Count);
        await Send.OkAsync(standings, ct);
    }
}
