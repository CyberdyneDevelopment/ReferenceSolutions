using Fdw.Collections.Attributes;
using Fdw.WebMcp.Abstractions;

namespace FdwReferenceAuth.Endpoints.IdentityEndpointOptions;

/// <summary>Declares the client-credentials create endpoint.</summary>
/// <remarks>
/// [TypeOption], not [Implementation]: an endpoint is a member of an endpoint collection, and the
/// service attribute registers nothing here.
/// </remarks>
[WebMcpTool("create_client_credentials_identity", "Create client credentials identity.")]
[TypeOption(typeof(IdentityEndpoints), "CreateClientCredentialsIdentity")]
public sealed class CreateClientCredentialsIdentityOption
    : IdentityEndpointBase<CreateClientCredentialsIdentityEndpoint>
{
}
