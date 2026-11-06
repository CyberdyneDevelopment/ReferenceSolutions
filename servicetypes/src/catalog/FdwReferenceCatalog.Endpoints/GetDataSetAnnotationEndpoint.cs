using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Quality;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Tags;

namespace FdwReferenceCatalog.Endpoints;

/// <summary>
/// Concrete endpoint to get a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataSetAnnotationEndpoint : GetDataSetAnnotationEndpointBase
{
    /// <inheritdoc />
    public GetDataSetAnnotationEndpoint(IDataSetAnnotationConfigurationProvider provider, IDataSetConfigurationProvider dataSets, ITagService tags)
        : base(provider, dataSets, tags)
    {
    }
}
