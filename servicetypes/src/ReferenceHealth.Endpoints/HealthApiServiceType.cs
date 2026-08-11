using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceHealth.Endpoints.HealthEndpointOptions;

namespace ReferenceHealth.Endpoints;

/// <summary>The health domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Health")]
public class HealthApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="HealthApiServiceType"/> class.</summary>
    public HealthApiServiceType()
        : base("Health", "Health", "Health API", "HTTP endpoints for the health domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new HealthEndpoints() };
}
