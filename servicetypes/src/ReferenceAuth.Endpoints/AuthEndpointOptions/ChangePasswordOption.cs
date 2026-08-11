using Fdw.Collections.Attributes;

namespace ReferenceAuth.Endpoints.AuthEndpointOptions;

/// <summary>The ChangePassword endpoint.</summary>
[TypeOption(typeof(AuthEndpoints), "ChangePassword")]
public class ChangePasswordOption : AuthEndpointBase<ChangePasswordEndpoint>
{
}
