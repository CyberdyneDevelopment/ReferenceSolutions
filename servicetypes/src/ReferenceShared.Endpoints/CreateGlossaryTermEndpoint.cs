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
/// Concrete endpoint to create a glossary term.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateGlossaryTermEndpoint : Fdw.Services.Catalog.Endpoints.CreateGlossaryTermEndpoint
{
    /// <inheritdoc />
    public CreateGlossaryTermEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
