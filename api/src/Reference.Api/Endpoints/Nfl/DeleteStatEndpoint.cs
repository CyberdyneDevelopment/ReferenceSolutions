using System.Diagnostics.CodeAnalysis;
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
/// Deletes a player game stat record.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteStatEndpoint : Endpoint<StatIdRequest>
{
    private readonly IDataGateway _dataGateway;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<DeleteStatEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteStatEndpoint"/> class.
    /// </summary>
    public DeleteStatEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider, ILogger<DeleteStatEndpoint> logger)
    {
        _dataGateway = dataGateway;
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
            AddError("Failed to resolve NflPlayerGameStats DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.NflOperationFailed(_logger, "DeleteStat", "No active source found for NflPlayerGameStats DataSet");
            AddError("No active source found for NflPlayerGameStats DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var command = new DeleteCommandBuilder(src.ContainerName)
            .DataStore(src.DataStoreName)
            .Path(src.Path)
            .Where("Id", req.StatId)
            .Build();

        var result = await _dataGateway.Execute<int>(command, ct);

        if (!result.IsSuccess)
        {
            NflLog.NflOperationFailed(_logger, "DeleteStat", result.CurrentMessage!);
            AddError("Failed to delete stat");
            await Send.ErrorsAsync(500, ct);
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
