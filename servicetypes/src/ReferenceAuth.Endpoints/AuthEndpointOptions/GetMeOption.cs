using Fdw.Collections.Attributes;

namespace ReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The GetMe endpoint.</summary>
[TypeOption(typeof(AuthEndpoints), "GetMe")]
public class GetMeOption : AuthEndpointBase<GetMeEndpoint>
{
}
