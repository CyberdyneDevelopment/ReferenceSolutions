using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSecretManagers.Endpoints.SecretManagerEndpointOptions;

/// <summary>The ListSecretManagers endpoint.</summary>
[WebMcpTool("list_secret_managers", "List secret managers.", ReadOnly = true)]
[TypeOption(typeof(SecretManagerEndpoints), "ListSecretManagers")]
public class ListSecretManagersOption : SecretManagerEndpointBase<ListSecretManagersEndpoint>
{
}
