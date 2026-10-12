using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSecretManagers.Endpoints.SecretManagerEndpointOptions;

/// <summary>The DeleteSecretManager endpoint.</summary>
[WebMcpTool("delete_secret_manager", "Delete secret manager.")]
[TypeOption(typeof(SecretManagerEndpoints), "DeleteSecretManager")]
public class DeleteSecretManagerOption : SecretManagerEndpointBase<DeleteSecretManagerEndpoint>
{
}
