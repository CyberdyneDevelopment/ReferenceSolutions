using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAudit.Endpoints.AuditEndpointOptions;

/// <summary>The ListAdministrativeChanges endpoint (FDW-1586).</summary>
[WebMcpTool("list_administrative_changes", "List the administrative changes: who changed what, where and when.", ReadOnly = true)]
[TypeOption(typeof(AuditEndpoints), "ListAdministrativeChanges")]
public class ListAdministrativeChangesOption : AuditEndpointBase<ListAdministrativeChangesEndpoint>
{
}
