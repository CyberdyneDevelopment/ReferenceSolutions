using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Hosting.Extensions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions.Visualization;

namespace Reference.Api.Endpoints.Visualization;

/// <summary>
/// Computes statistical summaries (StatSet) for specified columns.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ComputeStatSetEndpoint : Endpoint<StatSetRequest, StatSetResponse>
{
    private readonly IStatSetService _statSet;
    // Why: clients may supply a DataSetName instead of ContainerName. The DataSet provider
    // already assembles the Sources hierarchy on Get(name), so we look up the dataset and
    // promote its first source's container info into the request before the service runs.
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeStatSetEndpoint"/> class.
    /// </summary>
    public ComputeStatSetEndpoint(
        IStatSetService statSet,
        DataSetConfigurationProvider dataSetProvider)
    {
        _statSet = statSet;
        _dataSetProvider = dataSetProvider;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/data-preview/statset");
        Policies("datasets:read");
        Summary(s => s.Summary = "Compute StatSet for a query");
        Tags("Calculations");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(StatSetRequest req, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(req.ContainerName) && !string.IsNullOrEmpty(req.DataSetName))
        {
            var dataSetResult = await _dataSetProvider.Get(req.DataSetName, ct).ConfigureAwait(false);
            if (!dataSetResult.IsSuccess || dataSetResult.Value is null)
            {
                await HttpContext.WriteNotFound($"DataSet '{req.DataSetName}' was not found.", ct).ConfigureAwait(false);
                return;
            }

            var firstSource = dataSetResult.Value.Sources.FirstOrDefault();
            if (firstSource is null)
            {
                await HttpContext.WriteBadRequest("NoDataSetSource", $"DataSet '{req.DataSetName}' has no sources.", ct).ConfigureAwait(false);
                return;
            }

            req.ContainerName = !string.IsNullOrEmpty(firstSource.ContainerName)
                ? firstSource.ContainerName
                : firstSource.SourceName;
            req.ConnectionName = firstSource.ConnectionName;
            req.DataStoreName = firstSource.DataStoreName;
        }

        var result = await _statSet.ComputeStatSet(req, ct);

        if (!result.IsSuccess)
        {
            AddError("Failed to compute statistics");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        await Send.OkAsync(result.Value!, ct);
    }
}
