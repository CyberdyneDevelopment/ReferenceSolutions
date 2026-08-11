using Fdw.Collections.Attributes;

namespace ReferenceSecretManagers.Endpoints.SecretManagerEndpointOptions;

/// <summary>The CreateSecretManager endpoint.</summary>
[TypeOption(typeof(SecretManagerEndpoints), "CreateSecretManager")]
public class CreateSecretManagerOption : SecretManagerEndpointBase<CreateSecretManagerEndpoint>
{
}
