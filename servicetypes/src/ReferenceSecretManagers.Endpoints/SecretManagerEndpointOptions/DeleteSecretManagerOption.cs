using Fdw.Collections.Attributes;

namespace ReferenceSecretManagers.Endpoints.SecretManagerEndpointOptions;

/// <summary>The DeleteSecretManager endpoint.</summary>
[TypeOption(typeof(SecretManagerEndpoints), "DeleteSecretManager")]
public class DeleteSecretManagerOption : SecretManagerEndpointBase<DeleteSecretManagerEndpoint>
{
}
