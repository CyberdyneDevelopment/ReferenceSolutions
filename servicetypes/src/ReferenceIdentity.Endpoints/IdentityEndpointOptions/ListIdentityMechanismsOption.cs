using Fdw.Collections.Attributes;

namespace ReferenceIdentity.Endpoints.IdentityEndpointOptions;

/// <summary>Declares the ListIdentityMechanisms endpoint.</summary>
[TypeOption(typeof(IdentityEndpoints), "ListIdentityMechanisms")]
public sealed class ListIdentityMechanismsOption : IdentityEndpointBase<ListIdentityMechanismsEndpoint>
{
}
