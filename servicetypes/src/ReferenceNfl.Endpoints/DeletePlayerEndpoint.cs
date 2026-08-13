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
using ReferenceNfl.Endpoints.Logging;

namespace ReferenceNfl.Endpoints;

/// <summary>
/// Deletes a player record.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeletePlayerEndpoint : Endpoint<PlayerIdRequest>
{
    private readonly IDataGateway _dataGateway;
    private readonly DataSetConfigurationProvider _dataSetProvider;
    private readonly ILogger<DeletePlayerEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeletePlayerEndpoint"/> class.
    /// </summary>
    public DeletePlayerEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider, ILogger<DeletePlayerEndpoint> logger)
    {
        _dataGateway = dataGateway;
        _dataSetProvider = dataSetProvider;
        _logger = logger ?? NullLogger<DeletePlayerEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Delete("/nfl/players/{PlayerId}");
        Policies("datasets:write");
        Summary(s => s.Summary = "Delete a player");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(PlayerIdRequest req, CancellationToken ct)
    {
        NflLog.DeletingPlayer(_logger, req.PlayerId);

        var dsResult = await _dataSetProvider.Get("NflPlayers", ct);
        if (!dsResult.IsSuccess)
        {
            NflLog.DeletePlayerFailed(_logger, req.PlayerId, "Failed to resolve NflPlayers DataSet");
            AddError("Failed to resolve NflPlayers DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var src = dsResult.Value?.Sources.Where(s => s.IsCurrent && !s.IsDeleted).OrderBy(s => s.Priority).FirstOrDefault();
        if (src is null)
        {
            NflLog.DeletePlayerFailed(_logger, req.PlayerId, "No active source found for NflPlayers DataSet");
            AddError("No active source found for NflPlayers DataSet");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        var command = new DeleteCommandBuilder(src.ContainerName)
            .DataStore(src.DataStoreName)
            .Path(src.PathValue)
            .Where("Id", req.PlayerId)
            .Build();

        var result = await _dataGateway.Execute<int>(command, ct);

        if (!result.IsSuccess)
        {
            NflLog.DeletePlayerFailed(_logger, req.PlayerId, result.CurrentMessage!);
            AddError("Failed to delete player");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        // Why: rowsAffected == 0 means the player wasn't found; still succeeds at the HTTP level
        // but we log it as a warning for observability.
        if (result.Value == 0)
        {
            NflLog.DeletePlayerNotFound(_logger, req.PlayerId);
        }
        else
        {
            NflLog.PlayerDeleted(_logger, req.PlayerId);
        }

        await Send.NoContentAsync(ct);
    }
}
