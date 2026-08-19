using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Identity.Endpoints;

namespace ReferenceIdentity.Endpoints;

/// <summary><c>GET identities/mechanisms</c> — lists the mechanisms an identity can be backed by.</summary>
/// <remarks>Reads the TypeCollection, so it needs nothing injected.</remarks>
[ExcludeFromCodeCoverage]
public sealed class ListIdentityMechanismsEndpoint : ListIdentityMechanismsEndpointBase
{
}
