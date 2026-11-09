using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Commands.Data.Extensions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceNfl.Endpoints.Logging;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Fdw.Results;

namespace FdwReferenceNfl.Endpoints;

/// <summary>
/// Deletes a player game stat record.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteStatEndpoint : Endpoint<StatIdRequest>
{
    private readonly IDataGatewayProvider _dataGateways;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<DeleteStatEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteStatEndpoint"/> class.
    /// </summary>
    public DeleteStatEndpoint(IDataGatewayProvider dataGateways, DataSetConfigurationProvider dataSetProvider, ILogger<DeleteStatEndpoint> logger)
    {
        _dataGateways = dataGateways;
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<DeleteStatEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete("/nfl/stats/{StatId}");
        Policies("datasets:write");
        Summary(s => s.Summary = "Delete a player game stat");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(StatIdRequest req, CancellationToken ct)
    {
        NflLog.DeletingStat(_logger, req.StatId);

        var dsResult = await _dataSetProvider.Get("NflPlayerGameStats", ct);
        if (!dsResult.IsSuccess)
        {
            NflLog.NflOperationFailed(_logger, "DeleteStat", "Failed to resolve NflPlayerGameStats DataSet");
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(dsResult, HttpContext)).ConfigureAwait(false);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.NflOperationFailed(_logger, "DeleteStat", "No active source found for NflPlayerGameStats DataSet");
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(NflLog.DeleteStatReturnedNothing(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var command = new DeleteCommandBuilder(src.ContainerName)
            .DataStore(src.DataStoreName)
            .Path(src.PathValue)
            .Where("Id", req.StatId)
            .Build();

        var result = await _dataGateways.ByName("Main").Execute<int>(command, ct);

        if (!result.IsSuccess)
        {
            NflLog.NflOperationFailed(_logger, "DeleteStat", result.CurrentMessage!);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(result, HttpContext)).ConfigureAwait(false);
            return;
        }

        // Why: rowsAffected == 0 means the stat wasn't found; still succeeds at the HTTP level.
        if (result.Value == 0)
        {
            NflLog.DeleteStatNotFound(_logger, req.StatId);
        }
        else
        {
            NflLog.StatDeleted(_logger, req.StatId);
        }

        await Send.NoContentAsync(ct);
    }
}
