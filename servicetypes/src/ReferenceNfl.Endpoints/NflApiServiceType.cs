using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceNfl.Endpoints.NflEndpointOptions;

namespace ReferenceNfl.Endpoints;

/// <summary>The nfl domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Nfl")]
public class NflApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="NflApiServiceType"/> class.</summary>
    public NflApiServiceType()
        : base("Nfl", "Nfl", "Nfl API", "HTTP endpoints for the nfl domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new NflEndpoints() };
}
