using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceRoles.Endpoints.PermissionEndpointOptions;
using ReferenceRoles.Endpoints.RoleEndpointOptions;
using ReferenceRoles.Endpoints.RolePermissionEndpointOptions;

namespace ReferenceRoles.Endpoints;

/// <summary>
/// The roles domain's API surface.
/// </summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Roles")]
public class RolesApiServiceType : ApiServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RolesApiServiceType"/> class.
    /// </summary>
    public RolesApiServiceType()
        : base("Roles", "Roles", "Roles API", "HTTP endpoints for the roles domain.")
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
            new RoleEndpoints(),
            new PermissionEndpoints(),
            new RolePermissionEndpoints(),
        };
}
