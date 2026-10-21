using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: list the retention policies of an organization (FDW-1575).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListRetentionPoliciesEndpoint : ListRetentionPoliciesEndpointBase
{
    /// <inheritdoc />
    public ListRetentionPoliciesEndpoint(
        IRetentionPolicies policies,
        IOrganizationProvider organizations,
        ISystemRoleConfiguration systemRoles,
        ILogger<ListRetentionPoliciesEndpoint> logger)
        : base(logger, policies, organizations, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("retention:read");
        Summary(s =>
        {
            s.Summary = "List the retention policies of an organization";
            s.Description = "Lists the retention policies of an organization: how long each document type is kept, the date the period counts from and the end action.";
        });
        Tags("Retention");
    }
}
