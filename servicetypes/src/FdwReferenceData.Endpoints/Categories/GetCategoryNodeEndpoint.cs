using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to read a category.
/// Sealed closure of the base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetCategoryNodeEndpoint : GetCategoryEndpointBase
{
    /// <inheritdoc />
    public GetCategoryNodeEndpoint(CategoryConfigurationProvider categories, ILogger<GetCategoryNodeEndpoint> logger)
        : base(logger, categories)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Categories");
    }
}
