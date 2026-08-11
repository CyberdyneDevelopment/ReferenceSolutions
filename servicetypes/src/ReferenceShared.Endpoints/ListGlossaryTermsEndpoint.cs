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
/// Concrete endpoint to list glossary terms.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListGlossaryTermsEndpoint : Fdw.Services.Catalog.Endpoints.ListGlossaryTermsEndpoint
{
    /// <inheritdoc />
    public ListGlossaryTermsEndpoint(QualityConfigurationProvider provider)
        : base(provider)
    {
    }
}
