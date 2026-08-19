using Fdw.Collections.Attributes;

namespace ReferenceIdentity.Endpoints.IdentityEndpointOptions;

/// <summary>Declares the ListIdentities endpoint.</summary>
[TypeOption(typeof(IdentityEndpoints), "ListIdentities")]
public sealed class ListIdentitiesOption : IdentityEndpointBase<ListIdentitiesEndpoint>
{
}
