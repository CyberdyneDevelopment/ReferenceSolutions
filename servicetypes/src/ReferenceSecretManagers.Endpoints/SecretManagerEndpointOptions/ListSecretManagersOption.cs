using Fdw.Collections.Attributes;

namespace ReferenceSecretManagers.Endpoints.SecretManagerEndpointOptions;

/// <summary>The ListSecretManagers endpoint.</summary>
[TypeOption(typeof(SecretManagerEndpoints), "ListSecretManagers")]
public class ListSecretManagersOption : SecretManagerEndpointBase<ListSecretManagersEndpoint>
{
}
