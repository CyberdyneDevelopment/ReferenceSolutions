using Fdw.Collections.Attributes;

namespace ReferenceSecretManagers.Endpoints.SecretManagerTypeEndpointOptions;

/// <summary>The ListSecretManagerTypes endpoint.</summary>
[TypeOption(typeof(SecretManagerTypeEndpoints), "ListSecretManagerTypes")]
public class ListSecretManagerTypesOption : SecretManagerTypeEndpointBase<ListSecretManagerTypesEndpoint>
{
}
