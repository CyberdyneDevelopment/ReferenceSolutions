using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Quality;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to update a glossary term.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateGlossaryTermEndpoint : UpdateGlossaryTermEndpointBase
{
    /// <inheritdoc />
    public UpdateGlossaryTermEndpoint(IGlossaryTermConfigurationProvider provider, ICategoryConfigurationProvider categories)
        : base(provider, categories)
    {
    }
}
