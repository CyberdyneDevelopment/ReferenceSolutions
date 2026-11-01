using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: list the feature catalogue (FDW-1580).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListFeatureCatalogueEndpoint : ListFeatureCatalogueEndpointBase
{
    /// <inheritdoc />
    public ListFeatureCatalogueEndpoint(
        ILogger<ListFeatureCatalogueEndpoint> logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:read");
        Summary(s =>
        {
            s.Summary = "List the feature catalogue";
            s.Description = "Lists every feature the system knows, with what each gives.";
        });
        Tags("Tenants");
    }
}
