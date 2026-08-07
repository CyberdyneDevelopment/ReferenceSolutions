using Fdw.Operations.Endpoints.Audit;
using Fdw.Services.Audit.Abstractions;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Audit;

/// <summary>
/// Lists audit records with optional filtering by entity type, action, user, and date range.
/// </summary>
public sealed class ListAuditRecordsEndpoint : ListAuditRecordsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListAuditRecordsEndpoint"/> class.
    /// </summary>
    public ListAuditRecordsEndpoint(IAuditService auditService, ILogger<ListAuditRecordsEndpointBase>? logger)
        : base(auditService, logger) { }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint() => Tags("Audit");
}
