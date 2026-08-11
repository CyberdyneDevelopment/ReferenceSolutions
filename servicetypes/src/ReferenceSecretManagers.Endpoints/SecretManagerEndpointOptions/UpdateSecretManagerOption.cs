using Fdw.Collections.Attributes;

namespace ReferenceSecretManagers.Endpoints.SecretManagerEndpointOptions;

/// <summary>The UpdateSecretManager endpoint.</summary>
[TypeOption(typeof(SecretManagerEndpoints), "UpdateSecretManager")]
public class UpdateSecretManagerOption : SecretManagerEndpointBase<UpdateSecretManagerEndpoint>
{
}
