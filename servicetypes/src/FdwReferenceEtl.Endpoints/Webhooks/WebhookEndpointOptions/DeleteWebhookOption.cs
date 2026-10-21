using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.WebhookEndpointOptions;

/// <summary>The DeleteWebhook endpoint.</summary>
[TypeOption(typeof(WebhookEndpoints), "DeleteWebhook")]
public class DeleteWebhookOption : WebhookEndpointBase<DeleteWebhookEndpoint>
{
}
