using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceVisualization.Endpoints.VisualizationEndpointOptions;

namespace ReferenceVisualization.Endpoints;

/// <summary>The visualization domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Visualization")]
public class VisualizationApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="VisualizationApiServiceType"/> class.</summary>
    public VisualizationApiServiceType()
        : base("Visualization", "Visualization", "Visualization API", "HTTP endpoints for the visualization domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new VisualizationEndpoints() };
}
