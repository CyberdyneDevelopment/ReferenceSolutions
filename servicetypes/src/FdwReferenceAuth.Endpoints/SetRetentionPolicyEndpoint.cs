using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: set the retention policy of a document type (FDW-1575).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SetRetentionPolicyEndpoint : SetRetentionPolicyEndpointBase
{
    /// <inheritdoc />
    public SetRetentionPolicyEndpoint(
        IRetentionPolicies policies,
        IOrganizationProvider organizations,
        IAdministrativeAudit audit,
        ISystemRoleConfiguration systemRoles,
        ILogger<SetRetentionPolicyEndpoint> logger)
        : base(logger, policies, organizations, audit, systemRoles)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("retention:write");
        Summary(s =>
        {
            s.Summary = "Set the retention policy of a document type";
            s.Description = "Sets how long an organization keeps documents of one type, the document date the period counts from and what happens at the end. The change is audited.";
        });
        Tags("Retention");
    }
}
