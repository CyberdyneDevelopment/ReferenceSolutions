using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceProxy.Endpoints.ProxyEndpointOptions;

namespace ReferenceProxy.Endpoints;

/// <summary>The proxy domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Proxy")]
public class ProxyApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ProxyApiServiceType"/> class.</summary>
    public ProxyApiServiceType()
        : base("Proxy", "Proxy", "Proxy API", "HTTP endpoints for the proxy domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new ProxyEndpoints() };
}
