using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Hosting.Extensions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions.Visualization;

namespace ReferenceVisualization.Endpoints;

/// <summary>
/// Computes grouped statistical summaries (StatSet) with dimensions.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ComputeGroupedStatSetEndpoint : Endpoint<GroupedStatSetRequest, GroupedStatSetResponse>
{
    private readonly IStatSetService _statSet;
    // Why: same dataSetName → container resolution as the non-grouped endpoint.
    private readonly DataSetConfigurationProvider _dataSetProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputeGroupedStatSetEndpoint"/> class.
    /// </summary>
    public ComputeGroupedStatSetEndpoint(
        IStatSetService statSet,
        DataSetConfigurationProvider dataSetProvider)
    {
        _statSet = statSet;
        _dataSetProvider = dataSetProvider;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/data-preview/statset/grouped");
        Policies("datasets:read");
        Summary(s => s.Summary = "Compute grouped StatSet with dimensions");
        Tags("Calculations");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(GroupedStatSetRequest req, CancellationToken ct)
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

        var result = await _statSet.ComputeGroupedStatSet(req, ct);

        if (!result.IsSuccess)
        {
            AddError("Failed to compute grouped statistics");
            await Send.ErrorsAsync(500, ct);
            return;
        }

        await Send.OkAsync(result.Value!, ct);
    }
}
