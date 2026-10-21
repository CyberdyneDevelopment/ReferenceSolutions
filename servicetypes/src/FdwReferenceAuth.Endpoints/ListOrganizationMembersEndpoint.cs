using System.Diagnostics.CodeAnalysis;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint: list the people in an organization (FDW-1593).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListOrganizationMembersEndpoint : ListOrganizationMembersEndpointBase
{
    /// <inheritdoc />
    public ListOrganizationMembersEndpoint(
        IOrganizationMembers members,
        ILogger<ListOrganizationMembersEndpoint> logger)
        : base(logger, members)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Policies("organizations:read");
        Summary(s =>
        {
            s.Summary = "List the people in an organization";
            s.Description = "Lists the people in the organization and the role each holds there. The caller must act in the organization.";
        });
        Tags("Organizations");
    }
}
