using Fdw.Collections.Attributes;

namespace ReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The ConnectToken endpoint.</summary>
[TypeOption(typeof(AuthEndpoints), "ConnectToken")]
public class ConnectTokenOption : AuthEndpointBase<ConnectTokenEndpoint>
{
}
