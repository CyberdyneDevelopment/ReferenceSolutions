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
/// Concrete endpoint to get a glossary term by ID.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetGlossaryTermEndpoint : Fdw.Services.Catalog.Endpoints.GetGlossaryTermEndpoint
{
    /// <inheritdoc />
    public GetGlossaryTermEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
