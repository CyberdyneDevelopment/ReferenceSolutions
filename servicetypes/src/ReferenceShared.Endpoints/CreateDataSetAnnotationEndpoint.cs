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
/// Concrete endpoint to create a dataset annotation.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateDataSetAnnotationEndpoint : Fdw.Services.Catalog.Endpoints.CreateDataSetAnnotationEndpoint
{
    /// <inheritdoc />
    public CreateDataSetAnnotationEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
