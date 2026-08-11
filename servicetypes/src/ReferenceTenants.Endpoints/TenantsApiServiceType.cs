using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceTenants.Endpoints.TenantsEndpointOptions;

namespace ReferenceTenants.Endpoints;

/// <summary>The tenants domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Tenants")]
public class TenantsApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TenantsApiServiceType"/> class.</summary>
    public TenantsApiServiceType()
        : base("Tenants", "Tenants", "Tenants API", "HTTP endpoints for the tenants domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new TenantsEndpoints() };
}
