using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: give a person a role in an organization (FDW-1593).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GiveOrganizationMemberRoleEndpoint : GiveOrganizationMemberRoleEndpointBase
{
    /// <inheritdoc />
    public GiveOrganizationMemberRoleEndpoint(
        IOrganizationMembers members,
        IUserConfigurationProvider users,
        IAdministrativeAudit audit,
        ILogger<GiveOrganizationMemberRoleEndpoint> logger)
        : base(logger, members, users, audit)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:write");
        Summary(s =>
        {
            s.Summary = "Give a person a role in an organization";
            s.Description = "Adds the person to the organization or changes the role they hold there; they hold one role per organization. They must already have an account.";
        });
        Tags("Organizations");
    }
}
