using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceSecretManagers.Endpoints.SecretManagerEndpointOptions;
using ReferenceSecretManagers.Endpoints.SecretManagerTypeEndpointOptions;

namespace ReferenceSecretManagers.Endpoints;

/// <summary>The secret-managers domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "SecretManagers")]
public class SecretManagersApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SecretManagersApiServiceType"/> class.</summary>
    public SecretManagersApiServiceType()
        : base("SecretManagers", "SecretManagers", "SecretManagers API", "HTTP endpoints for the secret-managers domain.")
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
            new SecretManagerEndpoints(),
            new SecretManagerTypeEndpoints(),
        };
}
