using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Tags;
using Fdw.Services.Tags.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceTags.Endpoints;

/// <summary>Concrete endpoint: delete a tag and its links.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name -- auto-discovery is off, so a base with no declared concrete compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class DeleteTagEndpoint : DeleteTagEndpointBase
{
    /// <inheritdoc />
    public DeleteTagEndpoint(ILogger<DeleteTagEndpointBase> logger, ITagService tags, ITagConfigurationProvider provider)
        : base(logger, tags, provider)
    {
    }
}
