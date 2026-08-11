using Fdw.Collections.Attributes;

namespace ReferenceUsers.Endpoints.UserEndpointOptions;

/// <summary>The ResetPassword endpoint.</summary>
[TypeOption(typeof(UserEndpoints), "ResetPassword")]
public class ResetPasswordOption : UserEndpointBase<ResetPasswordEndpoint>
{
}
