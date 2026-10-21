using Fdw.Collections.Attributes;

namespace FdwReferencePipelines.Endpoints.WebhookEndpointOptions;

/// <summary>The NewWebhookSecret endpoint.</summary>
[TypeOption(typeof(WebhookEndpoints), "NewWebhookSecret")]
public class NewWebhookSecretOption : WebhookEndpointBase<NewWebhookSecretEndpoint>
{
}
