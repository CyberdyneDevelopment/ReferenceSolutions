using Fdw.Collections.Attributes;

namespace ReferenceIdentity.Endpoints.IdentityEndpointOptions;

/// <summary>Declares the VerifyIdentity endpoint.</summary>
[TypeOption(typeof(IdentityEndpoints), "VerifyIdentity")]
public sealed class VerifyIdentityOption : IdentityEndpointBase<VerifyIdentityEndpoint>
{
}
