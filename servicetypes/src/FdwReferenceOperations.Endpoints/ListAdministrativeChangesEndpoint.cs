using Fdw.Operations.Abstractions.Audit;
using Fdw.Operations.Endpoints.Audit;
using FdwReferenceAudit.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAudit.Endpoints;

/// <summary>
/// Lists the administrative changes (<c>GET /audit/changes</c>, FDW-1586): who changed what, at which level, the old and new value, and when.
/// </summary>
/// <remarks>
/// The route, the <c>audit:read</c> policy and the tenant scoping of the read belong to the base and to <see cref="IAdministrativeAudit"/>:
/// a platform administrator is answered every tenant's changes, anyone else only the tenant they act in.
/// </remarks>
public sealed class ListAdministrativeChangesEndpoint : ListAdministrativeChangesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListAdministrativeChangesEndpoint"/> class.</summary>
    /// <param name="audit">Reads the recorded changes.</param>
    /// <param name="logger">The logger.</param>
    public ListAdministrativeChangesEndpoint(IAdministrativeAudit audit, ILogger<ListAdministrativeChangesEndpoint>? logger = null)
        : base(logger ?? NullLogger<ListAdministrativeChangesEndpoint>.Instance, audit)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Audit");
    }

    /// <inheritdoc />
    protected override void OnListingChanges(int page, int pageSize)
        => AuditEndpointLog.Listing(EndpointLogger, page, pageSize);

    /// <inheritdoc />
    protected override void OnChangesListed(int count)
        => AuditEndpointLog.Listed(EndpointLogger, count);
}
