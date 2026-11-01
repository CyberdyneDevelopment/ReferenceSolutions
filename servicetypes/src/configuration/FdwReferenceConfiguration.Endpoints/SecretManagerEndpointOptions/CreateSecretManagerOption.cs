using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSecretManagers.Endpoints.SecretManagerEndpointOptions;

/// <summary>The CreateSecretManager endpoint.</summary>
[WebMcpTool("create_secret_manager", "Create secret manager.")]
[TypeOption(typeof(SecretManagerEndpoints), "CreateSecretManager")]
public class CreateSecretManagerOption : SecretManagerEndpointBase<CreateSecretManagerEndpoint>
{
}
