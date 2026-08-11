using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceConfiguration.Endpoints.ConfigurationCategoryEndpointOptions;
using ReferenceConfiguration.Endpoints.ConfigurationInstanceEndpointOptions;
using ReferenceConfiguration.Endpoints.ConfigurationTypeEndpointOptions;

namespace ReferenceConfiguration.Endpoints;

/// <summary>The configuration domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Configuration")]
public class ConfigurationApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ConfigurationApiServiceType"/> class.</summary>
    public ConfigurationApiServiceType()
        : base("Configuration", "Configuration", "Configuration API", "HTTP endpoints for the configuration domain.")
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
            new ConfigurationTypeEndpoints(),
            new ConfigurationInstanceEndpoints(),
            new ConfigurationCategoryEndpoints(),
        };
}
