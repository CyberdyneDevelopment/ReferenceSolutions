using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceNodes.Endpoints.NodesEndpointOptions;

namespace ReferenceNodes.Endpoints;

/// <summary>The nodes domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Nodes")]
public class NodesApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="NodesApiServiceType"/> class.</summary>
    public NodesApiServiceType()
        : base("Nodes", "Nodes", "Nodes API", "HTTP endpoints for the nodes domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new NodesEndpoints() };
}
