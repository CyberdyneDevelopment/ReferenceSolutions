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
/// Concrete endpoint to delete a glossary term.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteGlossaryTermEndpoint : Fdw.Services.Catalog.Endpoints.DeleteGlossaryTermEndpoint
{
    /// <inheritdoc />
    public DeleteGlossaryTermEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
