using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.WebhookEndpointOptions;

/// <summary>The UpdateWebhook endpoint.</summary>
[TypeOption(typeof(WebhookEndpoints), "UpdateWebhook")]
public class UpdateWebhookOption : WebhookEndpointBase<UpdateWebhookEndpoint>
{
}
