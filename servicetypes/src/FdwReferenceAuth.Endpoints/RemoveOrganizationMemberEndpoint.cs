using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: remove a person from an organization (FDW-1593).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RemoveOrganizationMemberEndpoint : RemoveOrganizationMemberEndpointBase
{
    private readonly UserTenantConfigurationProvider _userTenants;
    private readonly IOrganizationProvider _organizations;

    /// <inheritdoc />
    public RemoveOrganizationMemberEndpoint(
        IOrganizationMembers members,
        IAdministrativeAudit audit,
        UserTenantConfigurationProvider userTenants,
        IOrganizationProvider organizations,
        ILogger<RemoveOrganizationMemberEndpoint> logger)
        : base(logger, members, audit)
    {
        _userTenants = userTenants;
        _organizations = organizations;
    }

    // Why: a person whose access to an organization is removed no longer holds it as their default (FDW-2938); a default that stayed would
    // make their next sign-in refuse.
    /// <inheritdoc />
    protected override async Task<IGenericResult> AfterRemoved(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var organization = await _organizations.Get(organizationId, cancellationToken).ConfigureAwait(false);
        if (organization.IsFailure || organization.Value is null)
        {
            return organization;
        }

        return await DefaultOrganizationMembership
            .ClearIfDefault(_userTenants, userId, organization.Value.TenantId, organizationId, EndpointLogger, cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:write");
        Summary(s =>
        {
            s.Summary = "Remove a person from an organization";
            s.Description = "Takes the person out of the organization; they keep nothing there.";
        });
        Tags("Organizations");
    }
}
