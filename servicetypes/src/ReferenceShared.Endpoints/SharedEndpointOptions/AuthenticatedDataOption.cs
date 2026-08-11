using Fdw.Collections.Attributes;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The AuthenticatedData endpoint.</summary>
[TypeOption(typeof(SharedEndpoints), "AuthenticatedData")]
public class AuthenticatedDataOption : SharedEndpointBase<AuthenticatedDataEndpoint>
{
}
