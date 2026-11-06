using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Tags;
using Fdw.Services.Tags.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceTags.Endpoints;

/// <summary>Concrete endpoint: list the owners a tag can be given to (FDW-4100).</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name -- auto-discovery is off, so a base with no declared concrete compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ListTagOwnersEndpoint : ListTagOwnersEndpointBase
{
    /// <inheritdoc />
    public ListTagOwnersEndpoint(ILogger<ListTagOwnersEndpointBase> logger, ITagOwners owners)
        : base(logger, owners)
    {
    }
}
