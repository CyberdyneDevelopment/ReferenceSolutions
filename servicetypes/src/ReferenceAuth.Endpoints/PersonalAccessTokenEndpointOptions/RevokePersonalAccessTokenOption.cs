using Fdw.Collections.Attributes;

namespace ReferenceAuth.Endpoints.PersonalAccessTokenEndpointOptions;

/// <summary>The RevokePersonalAccessToken endpoint.</summary>
[TypeOption(typeof(PersonalAccessTokenEndpoints), "RevokePersonalAccessToken")]
public class RevokePersonalAccessTokenOption : PersonalAccessTokenEndpointBase<RevokePersonalAccessTokenEndpoint>
{
}
