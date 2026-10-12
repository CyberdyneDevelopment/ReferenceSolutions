using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceSecretManagers.Endpoints.SecretManagerEndpointOptions;

/// <summary>The UpdateSecretManager endpoint.</summary>
[WebMcpTool("update_secret_manager", "Update secret manager.")]
[TypeOption(typeof(SecretManagerEndpoints), "UpdateSecretManager")]
public class UpdateSecretManagerOption : SecretManagerEndpointBase<UpdateSecretManagerEndpoint>
{
}
