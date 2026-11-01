using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: list the document connections granted to a tenant (FDW-1581).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListConnectionGrantsEndpoint : ListConnectionGrantsEndpointBase
{
    /// <inheritdoc />
    public ListConnectionGrantsEndpoint(
        IConnectionGrants grants,
        ISystemRoleConfiguration systemRoles,
        ILogger<ListConnectionGrantsEndpoint> logger)
        : base(logger, grants, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:read");
        Summary(s =>
        {
            s.Summary = "List the document connections granted to a tenant";
            s.Description = "Lists the connections a tenant was granted. A platform administrator reads any tenant's; anyone else reads the tenant they act in.";
        });
        Tags("Tenants");
    }
}
