using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSecretManagers.Endpoints.SecretManagerEndpointOptions;

/// <summary>The GetSecretManager endpoint.</summary>
[WebMcpTool("get_secret_manager", "Get secret manager.", ReadOnly = true)]
[TypeOption(typeof(SecretManagerEndpoints), "GetSecretManager")]
public class GetSecretManagerOption : SecretManagerEndpointBase<GetSecretManagerEndpoint>
{
}
