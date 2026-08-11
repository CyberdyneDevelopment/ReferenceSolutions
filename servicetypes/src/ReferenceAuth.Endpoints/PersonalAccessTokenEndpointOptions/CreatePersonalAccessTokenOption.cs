using Fdw.Collections.Attributes;

namespace ReferenceAuth.Endpoints.PersonalAccessTokenEndpointOptions;

/// <summary>The CreatePersonalAccessToken endpoint.</summary>
[TypeOption(typeof(PersonalAccessTokenEndpoints), "CreatePersonalAccessToken")]
public class CreatePersonalAccessTokenOption : PersonalAccessTokenEndpointBase<CreatePersonalAccessTokenEndpoint>
{
}
