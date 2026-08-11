using Fdw.Collections.Attributes;

namespace ReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The GetExternalIdentityProviders endpoint.</summary>
[TypeOption(typeof(AuthEndpoints), "GetExternalIdentityProviders")]
public class GetExternalIdentityProvidersOption : AuthEndpointBase<GetExternalIdentityProvidersEndpoint>
{
}
