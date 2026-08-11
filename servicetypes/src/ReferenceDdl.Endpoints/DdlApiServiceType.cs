using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceDdl.Endpoints.DdlEndpointOptions;

namespace ReferenceDdl.Endpoints;

/// <summary>The ddl domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Ddl")]
public class DdlApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DdlApiServiceType"/> class.</summary>
    public DdlApiServiceType()
        : base("Ddl", "Ddl", "Ddl API", "HTTP endpoints for the ddl domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new DdlEndpoints() };
}
