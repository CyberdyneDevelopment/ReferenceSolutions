using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Identity.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary><c>GET identities/mechanisms</c> — lists the mechanisms an identity can be backed by.</summary>
/// <remarks>Reads the TypeCollection, so it needs nothing injected.</remarks>
[ExcludeFromCodeCoverage]
public sealed class ListIdentityMechanismsEndpoint : ListIdentityMechanismsEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListIdentityMechanismsEndpoint"/> class.</summary>
    public ListIdentityMechanismsEndpoint(ILogger<ListIdentityMechanismsEndpoint> logger) : base(logger)
    {
    }

}
