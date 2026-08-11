using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceTransformations.Endpoints.TransformationsEndpointOptions;

namespace ReferenceTransformations.Endpoints;

/// <summary>The transformations domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Transformations")]
public class TransformationsApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TransformationsApiServiceType"/> class.</summary>
    public TransformationsApiServiceType()
        : base("Transformations", "Transformations", "Transformations API", "HTTP endpoints for the transformations domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new TransformationsEndpoints() };
}
