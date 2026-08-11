using Fdw.Collections.Attributes;

namespace ReferenceAuth.Endpoints.PersonalAccessTokenEndpointOptions;

/// <summary>The ListPersonalAccessTokens endpoint.</summary>
[TypeOption(typeof(PersonalAccessTokenEndpoints), "ListPersonalAccessTokens")]
public class ListPersonalAccessTokensOption : PersonalAccessTokenEndpointBase<ListPersonalAccessTokensEndpoint>
{
}
