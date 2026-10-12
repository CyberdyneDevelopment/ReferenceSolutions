using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Quality;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to delete a glossary term.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteGlossaryTermEndpoint : DeleteGlossaryTermEndpointBase
{
    /// <inheritdoc />
    public DeleteGlossaryTermEndpoint(IGlossaryTermConfigurationProvider provider)
        : base(provider)
    {
    }
}
