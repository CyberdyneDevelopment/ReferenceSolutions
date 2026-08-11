using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEtlJobs.Endpoints.EtlJobsEndpointOptions;

namespace ReferenceEtlJobs.Endpoints;

/// <summary>The ETL jobs domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "EtlJobs")]
public class EtlJobsApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="EtlJobsApiServiceType"/> class.</summary>
    public EtlJobsApiServiceType()
        : base("EtlJobs", "EtlJobs", "EtlJobs API", "ETL server endpoints for the jobs domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new EtlJobsEndpoints() };
}
