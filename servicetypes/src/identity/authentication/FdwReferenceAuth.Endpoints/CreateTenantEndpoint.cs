using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Fdw.Services.Users;
using Fdw.Services.Users.Services;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to make a tenant together with its first administrator (FDW-1578).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateTenantEndpoint : CreateTenantEndpointBase
{
    /// <inheritdoc />
    public CreateTenantEndpoint(
        ITenantFounding founding,
        ITenantProvider tenantProvider,
        IUserConfigurationProvider userProvider,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoleConfiguration,
        ILogger<CreateTenantEndpoint> logger)
        : base(logger, founding, tenantProvider, userProvider, audit, systemRoleConfiguration)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("tenants:write");
        Summary(s =>
        {
            s.Summary = "Make a tenant";
            s.Description = "Makes a tenant and its first administrator, who must already have an account. Only a platform administrator may.";
        });
        Tags("Tenants");
    }
}
