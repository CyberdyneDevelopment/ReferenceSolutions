using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Operations.Endpoints;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceShared.Endpoints.SharedEndpointOptions;

namespace ReferenceShared.Endpoints;

/// <summary>The shared domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Shared")]
public class SharedApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SharedApiServiceType"/> class.</summary>
    public SharedApiServiceType()
        : base("Shared", "Shared", "Shared API", "HTTP endpoints for the shared domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
        {
            // GetDataflowGraphEndpoint is in this package and needs the provider. It used to sit in
            // Program.cs with a comment saying it stayed there "until an Operations.Endpoints
            // scaffold is stood up" - this package is that scaffold, so it comes home.
            DataflowGraphConfigurationProvider.RegisterDomainConfiguration(builder.Services);

            return RegisterEndpoints(builder, loggerFactory);
        });

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new SharedEndpoints() };
}
