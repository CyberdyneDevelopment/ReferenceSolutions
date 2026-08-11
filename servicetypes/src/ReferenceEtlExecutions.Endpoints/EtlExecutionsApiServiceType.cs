using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEtlExecutions.Endpoints.EtlExecutionsEndpointOptions;

namespace ReferenceEtlExecutions.Endpoints;

/// <summary>The ETL executions domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "EtlExecutions")]
public class EtlExecutionsApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="EtlExecutionsApiServiceType"/> class.</summary>
    public EtlExecutionsApiServiceType()
        : base("EtlExecutions", "EtlExecutions", "EtlExecutions API", "ETL server endpoints for the executions domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new EtlExecutionsEndpoints() };
}
