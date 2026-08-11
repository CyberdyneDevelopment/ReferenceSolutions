using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceLineage.Endpoints.LineageEndpointOptions;

namespace ReferenceLineage.Endpoints;

/// <summary>The lineage domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Lineage")]
public class LineageApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="LineageApiServiceType"/> class.</summary>
    public LineageApiServiceType()
        : base("Lineage", "Lineage", "Lineage API", "HTTP endpoints for the lineage domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new LineageEndpoints() };
}
