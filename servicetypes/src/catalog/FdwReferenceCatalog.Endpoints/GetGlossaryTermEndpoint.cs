using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Quality;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to get a glossary term by ID.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetGlossaryTermEndpoint : GetGlossaryTermEndpointBase
{
    /// <inheritdoc />
    public GetGlossaryTermEndpoint(IGlossaryTermConfigurationProvider provider, ICategoryConfigurationProvider categories)
        : base(provider, categories)
    {
    }
}
