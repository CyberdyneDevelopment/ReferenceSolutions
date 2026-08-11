using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEtlLineage.Endpoints.EtlLineageEndpointOptions;

namespace ReferenceEtlLineage.Endpoints;

/// <summary>The ETL lineage domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "EtlLineage")]
public class EtlLineageApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="EtlLineageApiServiceType"/> class.</summary>
    public EtlLineageApiServiceType()
        : base("EtlLineage", "EtlLineage", "EtlLineage API", "ETL server endpoints for the lineage domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new EtlLineageEndpoints() };
}
