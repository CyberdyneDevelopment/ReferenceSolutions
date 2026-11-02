using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to rename, describe or move a category.
/// Sealed closure of the base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateCategoryEndpoint : UpdateCategoryEndpointBase
{
    /// <inheritdoc />
    public UpdateCategoryEndpoint(CategoryConfigurationProvider categories, ILogger<UpdateCategoryEndpoint> logger)
        : base(logger, categories)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Categories");
    }
}
