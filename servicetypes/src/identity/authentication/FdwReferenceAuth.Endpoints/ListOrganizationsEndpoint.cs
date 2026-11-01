using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Multitenancy;
using Fdw.Services.Multitenancy.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to list the organizations of the caller's tenant: <c>GET /organizations</c> (FDW-1010, FDW-1016).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListOrganizationsEndpoint : ListOrganizationsEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListOrganizationsEndpoint"/> class.</summary>
    public ListOrganizationsEndpoint(IOrganizationConfigurationProvider organizations, ILogger<ListOrganizationsEndpoint>? logger)
        : base(logger ?? NullLogger<ListOrganizationsEndpoint>.Instance, organizations)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Organizations");
    }
}
