using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Catalog.Endpoints;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Quality;

namespace ReferenceShared.Endpoints;

/// <summary>
/// Concrete endpoint to get a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataSetAnnotationEndpoint : Fdw.Services.Catalog.Endpoints.GetDataSetAnnotationEndpoint
{
    /// <inheritdoc />
    public GetDataSetAnnotationEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
