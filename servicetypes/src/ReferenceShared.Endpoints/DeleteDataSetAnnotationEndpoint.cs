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
/// Concrete endpoint to delete a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteDataSetAnnotationEndpoint : Fdw.Services.Catalog.Endpoints.DeleteDataSetAnnotationEndpoint
{
    /// <inheritdoc />
    public DeleteDataSetAnnotationEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
