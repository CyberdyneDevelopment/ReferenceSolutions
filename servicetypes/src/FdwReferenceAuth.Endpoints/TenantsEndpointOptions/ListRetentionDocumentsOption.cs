using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.TenantsEndpointOptions;

/// <summary>The ListRetentionDocuments endpoint (FDW-2906).</summary>
[WebMcpTool("list_retention_documents", "List the documents of one type that belong to an organization, so a legal hold can be placed on one.")]
[TypeOption(typeof(TenantsEndpoints), "ListRetentionDocuments")]
public class ListRetentionDocumentsOption : TenantsEndpointBase<ListRetentionDocumentsEndpoint>
{
}
