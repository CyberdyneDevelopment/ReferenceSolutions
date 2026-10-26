using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: remove the retention policy of a document type (FDW-1575).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RemoveRetentionPolicyEndpoint : RemoveRetentionPolicyEndpointBase
{
    /// <inheritdoc />
    public RemoveRetentionPolicyEndpoint(
        IRetentionPolicies policies,
        IAdministrativeAudit audit,
        ILogger<RemoveRetentionPolicyEndpoint> logger)
        : base(logger, policies, audit)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("retention:write");
        Summary(s =>
        {
            s.Summary = "Remove the retention policy of a document type";
            s.Description = "Removes the retention policy of a document type, so the retention job no longer ends its documents. The removal is audited.";
        });
        Tags("Retention");
    }
}
