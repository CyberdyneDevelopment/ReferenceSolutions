using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferencePipelines.Endpoints.WebhookEndpointOptions;

/// <summary>The webhook trigger endpoints of the ETL host: only a host that references this package serves them (FDW-1884).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "WebhookEndpoints")]
[TypeCollection(typeof(WebhookEndpointBase), typeof(IEndpointTypeOption), typeof(WebhookEndpoints))]
public partial class WebhookEndpoints : EndpointTypeCollectionBase<WebhookEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
