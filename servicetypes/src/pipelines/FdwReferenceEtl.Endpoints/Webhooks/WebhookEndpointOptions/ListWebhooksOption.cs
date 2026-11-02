using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.WebhookEndpointOptions;

/// <summary>The ListWebhooks endpoint.</summary>
[TypeOption(typeof(WebhookEndpoints), "ListWebhooks")]
public class ListWebhooksOption : WebhookEndpointBase<ListWebhooksEndpoint>
{
}
