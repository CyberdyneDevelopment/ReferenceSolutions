using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceSchema.Endpoints.DataPreviewEndpointOptions;
using ReferenceSchema.Endpoints.SchemaConnectionEndpointOptions;
using ReferenceSchema.Endpoints.SchemaEndpointOptions;

namespace ReferenceSchema.Endpoints;

/// <summary>The schema domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Schema")]
public class SchemaApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SchemaApiServiceType"/> class.</summary>
    public SchemaApiServiceType()
        : base("Schema", "Schema", "Schema API", "HTTP endpoints for the schema domain.")
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
            new SchemaEndpoints(),
            new SchemaConnectionEndpoints(),
            new DataPreviewEndpoints(),
        };
}
