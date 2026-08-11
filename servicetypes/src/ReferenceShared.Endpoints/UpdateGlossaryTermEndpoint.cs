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
/// Concrete endpoint to update a glossary term.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateGlossaryTermEndpoint : Fdw.Services.Catalog.Endpoints.UpdateGlossaryTermEndpoint
{
    /// <inheritdoc />
    public UpdateGlossaryTermEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
