using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceFieldMappings.Endpoints.FieldMappingsEndpointOptions;

namespace ReferenceFieldMappings.Endpoints;

/// <summary>The fieldmappings domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "FieldMappings")]
public class FieldMappingsApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="FieldMappingsApiServiceType"/> class.</summary>
    public FieldMappingsApiServiceType()
        : base("FieldMappings", "FieldMappings", "FieldMappings API", "HTTP endpoints for the fieldmappings domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new FieldMappingsEndpoints() };
}
