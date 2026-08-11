using Fdw.Collections.Attributes;

namespace ReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The Logout endpoint.</summary>
[TypeOption(typeof(AuthEndpoints), "Logout")]
public class LogoutOption : AuthEndpointBase<LogoutEndpoint>
{
}
