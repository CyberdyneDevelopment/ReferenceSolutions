using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Quality;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to create a glossary term.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateGlossaryTermEndpoint : CreateGlossaryTermEndpointBase
{
    /// <inheritdoc />
    public CreateGlossaryTermEndpoint(IGlossaryTermConfigurationProvider provider, ICategoryConfigurationProvider categories)
        : base(provider, categories)
    {
    }
}
