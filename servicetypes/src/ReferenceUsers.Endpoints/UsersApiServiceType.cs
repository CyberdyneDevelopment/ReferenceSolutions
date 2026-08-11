using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceUsers.Endpoints.UserEndpointOptions;
using ReferenceUsers.Endpoints.UserPreferenceEndpointOptions;
using ReferenceUsers.Endpoints.UserRoleEndpointOptions;

namespace ReferenceUsers.Endpoints;

/// <summary>
/// The users domain's API surface.
/// </summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Users")]
public class UsersApiServiceType : ApiServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UsersApiServiceType"/> class.
    /// </summary>
    public UsersApiServiceType()
        : base("Users", "Users", "Users API", "HTTP endpoints for the users domain.")
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
            new UserEndpoints(),
            new UserPreferenceEndpoints(),
            new UserRoleEndpoints(),
        };
}
