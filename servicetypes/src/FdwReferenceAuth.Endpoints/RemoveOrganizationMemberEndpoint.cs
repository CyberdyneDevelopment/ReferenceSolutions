using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: remove a person from an organization (FDW-1593).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class RemoveOrganizationMemberEndpoint : RemoveOrganizationMemberEndpointBase
{
    /// <inheritdoc />
    public RemoveOrganizationMemberEndpoint(
        IOrganizationMembers members,
        IAdministrativeAudit audit,
        ILogger<RemoveOrganizationMemberEndpoint> logger)
        : base(logger, members, audit)
    {
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
