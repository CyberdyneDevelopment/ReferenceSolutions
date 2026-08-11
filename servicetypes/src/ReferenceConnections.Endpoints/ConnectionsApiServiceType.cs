using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceConnections.Endpoints.ConnectionEndpointOptions;
using ReferenceConnections.Endpoints.ConnectionTypeEndpointOptions;

namespace ReferenceConnections.Endpoints;

/// <summary>
/// The connections domain's API surface.
/// </summary>
/// <remarks>
/// Declared here rather than in the framework because the concrete endpoint types live here — the
/// framework ships the abstract endpoint bases and the mechanism, and the host closes both.
/// </remarks>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Connections")]
public class ConnectionsApiServiceType : ApiServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConnectionsApiServiceType"/> class.
    /// </summary>
    public ConnectionsApiServiceType()
        : base("Connections", "Connections", "Connections API", "HTTP endpoints for the connections domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[]
        {
            // Why an instance rather than the static collection: the sweep drives collections
            // polymorphically, and Members bridges to the generated static All().
            new ConnectionEndpoints(),
            new ConnectionTypeEndpoints(),
        };
}
