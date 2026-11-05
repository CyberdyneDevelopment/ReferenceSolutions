using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Tags;
using Fdw.Services.Tags.Endpoints;

namespace FdwReferenceTags.Endpoints;

/// <summary>Concrete endpoint: list the tags of a thing.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name -- auto-discovery is off, so a base with no declared concrete compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetThingTagsEndpoint : GetThingTagsEndpointBase
{
    /// <inheritdoc />
    public GetThingTagsEndpoint(ITagService tags)
        : base(tags)
    {
    }
}
