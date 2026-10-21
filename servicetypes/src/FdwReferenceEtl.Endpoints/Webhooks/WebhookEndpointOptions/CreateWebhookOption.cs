using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.WebhookEndpointOptions;

/// <summary>The CreateWebhook endpoint.</summary>
[TypeOption(typeof(WebhookEndpoints), "CreateWebhook")]
public class CreateWebhookOption : WebhookEndpointBase<CreateWebhookEndpoint>
{
}
