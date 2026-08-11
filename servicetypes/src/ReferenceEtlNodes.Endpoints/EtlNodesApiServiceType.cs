using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEtlNodes.Endpoints.EtlNodesEndpointOptions;

namespace ReferenceEtlNodes.Endpoints;

/// <summary>The ETL nodes domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "EtlNodes")]
public class EtlNodesApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="EtlNodesApiServiceType"/> class.</summary>
    public EtlNodesApiServiceType()
        : base("EtlNodes", "EtlNodes", "EtlNodes API", "ETL server endpoints for the nodes domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new EtlNodesEndpoints() };
}
