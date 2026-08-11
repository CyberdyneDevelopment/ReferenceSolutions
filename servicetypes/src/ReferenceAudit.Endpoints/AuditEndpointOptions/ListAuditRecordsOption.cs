using Fdw.Collections.Attributes;

namespace ReferenceAudit.Endpoints.AuditEndpointOptions;

/// <summary>The ListAuditRecords endpoint.</summary>
[TypeOption(typeof(AuditEndpoints), "ListAuditRecords")]
public class ListAuditRecordsOption : AuditEndpointBase<ListAuditRecordsEndpoint>
{
}
