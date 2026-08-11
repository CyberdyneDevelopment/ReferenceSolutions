using Fdw.Collections.Attributes;

namespace ReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The TokenSwitch endpoint.</summary>
[TypeOption(typeof(AuthEndpoints), "TokenSwitch")]
public class TokenSwitchOption : AuthEndpointBase<TokenSwitchEndpoint>
{
}
