using Fdw.Collections.Attributes;

namespace ReferenceIdentity.Endpoints.IdentityEndpointOptions;

/// <summary>Declares the Authentik client-credentials create endpoint.</summary>
/// <remarks>
/// [TypeOption], not [ServiceTypeOption]: an endpoint is a member of an endpoint collection, and the
/// service attribute registers nothing here.
/// </remarks>
[TypeOption(typeof(IdentityEndpoints), "CreateAuthentikClientCredentialsIdentity")]
public sealed class CreateAuthentikClientCredentialsIdentityOption
    : IdentityEndpointBase<CreateAuthentikClientCredentialsIdentityEndpoint>
{
}
