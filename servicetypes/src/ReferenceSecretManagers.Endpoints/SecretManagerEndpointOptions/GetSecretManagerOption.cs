using Fdw.Collections.Attributes;

namespace ReferenceSecretManagers.Endpoints.SecretManagerEndpointOptions;

/// <summary>The GetSecretManager endpoint.</summary>
[TypeOption(typeof(SecretManagerEndpoints), "GetSecretManager")]
public class GetSecretManagerOption : SecretManagerEndpointBase<GetSecretManagerEndpoint>
{
}
