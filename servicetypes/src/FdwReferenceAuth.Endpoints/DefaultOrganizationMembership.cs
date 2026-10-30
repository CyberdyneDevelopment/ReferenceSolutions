using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Users;
using Fdw.Services.Users.Configuration;
using FdwReferenceAuth.Endpoints.Logging;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Keeps the department a person lands in on their membership of a company (FDW-2938): the one place that finds the membership and saves it, for
/// the endpoint that sets the default and the one that clears it.
/// </summary>
internal static class DefaultOrganizationMembership
{
    /// <summary>The UserTenants domain, and its one implementation, as the domain row records them.</summary>
    private const string UserTenantsDomain = "UserTenants";

    /// <summary>
    /// Records a change to a person's default organization in the administrative audit. The change is made at the Tenant level, so the entry
    /// carries no organization field (the audit refuses one above the Organization level); the department marked is the entry's new value.
    /// </summary>
    /// <param name="audit">The administrative audit.</param>
    /// <param name="changeKind">What happened: the default was set or cleared.</param>
    /// <param name="userId">The person.</param>
    /// <param name="tenantId">The company.</param>
    /// <param name="organizationId">The department now marked, or none when it was cleared.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>Success, or the reason it could not be audited.</returns>
    internal static async Task<IGenericResult> Audit(
        IAdministrativeAudit audit, string changeKind, Guid userId, Guid tenantId, Guid? organizationId, ILogger logger, CancellationToken cancellationToken)
    {
        var recorded = await audit
            .Record(
                new AdministrativeChangeEntry(
                    changeKind, "UserDefaultOrganization", userId.ToString(), AdministrativeLevels.ByName("Tenant"), null, null, organizationId?.ToString(), null),
                cancellationToken)
            .ConfigureAwait(false);
        return recorded.IsSuccess
            ? recorded
            : GenericResult.Failure(TenantLog.DefaultOrganizationNotAudited(logger, userId.ToString(), tenantId, recorded.CurrentMessage));
    }

    // Clears the person's default only when it is this organization; a person with no membership row or another default is left as they are.
    internal static async Task<IGenericResult> ClearIfDefault(
        UserTenantConfigurationProvider provider, Guid userId, Guid tenantId, Guid organizationId, ILogger logger, CancellationToken cancellationToken)
    {
        var memberships = await provider
            .Find<IUserTenantImplementationConfiguration>(t => t.UserId == userId && t.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);
        if (!memberships.IsSuccess || memberships.Value is null)
        {
            return memberships;
        }

        if (memberships.Value.Count == 0 || memberships.Value[0].DefaultOrganizationId != organizationId)
        {
            return GenericResult.Success();
        }

        return await Keep(provider, userId, tenantId, null, logger, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Saves the department on the person's membership of the company.</summary>
    /// <param name="provider">Reads and writes the memberships.</param>
    /// <param name="userId">The person.</param>
    /// <param name="tenantId">The company.</param>
    /// <param name="organizationId">The department to land in, or none to land in the company with no department.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>Success, or the reason it could not be kept.</returns>
    internal static async Task<IGenericResult> Keep(
        UserTenantConfigurationProvider provider, Guid userId, Guid tenantId, Guid? organizationId, ILogger logger, CancellationToken cancellationToken)
    {
        var memberships = await provider
            .Find<IUserTenantImplementationConfiguration>(t => t.UserId == userId && t.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);
        if (!memberships.IsSuccess || memberships.Value is null)
        {
            return memberships;
        }

        if (memberships.Value.Count == 0)
        {
            return GenericResult.Failure(TenantLog.DefaultOrganizationNoMembership(logger, userId.ToString(), tenantId));
        }

        var membership = memberships.Value[0];
        membership.DefaultOrganizationId = organizationId;
        var saved = await provider
            .Update(membership, UserTenantsDomain, UserTenantsDomain, membership.Name, cancellationToken)
            .ConfigureAwait(false);
        return saved.IsSuccess
            ? saved
            : GenericResult.Failure(TenantLog.DefaultOrganizationNotSaved(logger, userId.ToString(), tenantId, saved.CurrentMessage));
    }
}
