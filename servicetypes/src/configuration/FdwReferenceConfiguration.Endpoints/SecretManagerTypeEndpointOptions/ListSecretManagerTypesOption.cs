using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSecretManagers.Endpoints.SecretManagerTypeEndpointOptions;

/// <summary>The ListSecretManagerTypes endpoint.</summary>
[WebMcpTool("list_secret_manager_types", "List secret manager types.", ReadOnly = true)]
[TypeOption(typeof(SecretManagerTypeEndpoints), "ListSecretManagerTypes")]
public class ListSecretManagerTypesOption : SecretManagerTypeEndpointBase<ListSecretManagerTypesEndpoint>
{
}
