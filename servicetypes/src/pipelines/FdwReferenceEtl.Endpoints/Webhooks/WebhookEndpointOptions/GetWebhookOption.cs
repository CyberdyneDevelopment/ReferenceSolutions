using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.WebhookEndpointOptions;

/// <summary>The GetWebhook endpoint.</summary>
[TypeOption(typeof(WebhookEndpoints), "GetWebhook")]
public class GetWebhookOption : WebhookEndpointBase<GetWebhookEndpoint>
{
}
