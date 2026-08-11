using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Data.Abstractions.Visualization;

namespace ReferenceVisualization.Endpoints;

/// <summary>
/// Lists all available visualization types from the VisualizationTypes TypeCollection.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListVisualizationTypesEndpoint : EndpointWithoutRequest<VisualizationTypeListResponse>
{
    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/visualization-types");
        Policies("datasets:read");
        Summary(s => s.Summary = "List available visualization types");
        Tags("Calculations");
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var types = VisualizationTypes.All()
            .Where(t => !string.Equals(t.Name, "_Empty", System.StringComparison.Ordinal))
            .Select(t => new VisualizationTypeItem
            {
                Name = t.Name,
                DisplayName = t.DisplayName,
                Icon = t.Icon
            })
            .ToList();

        await Send.OkAsync(new VisualizationTypeListResponse { Types = types }, ct);
    }
}
