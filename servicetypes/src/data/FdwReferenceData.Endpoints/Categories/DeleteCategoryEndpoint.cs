using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Quality;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to retire a category.
/// Sealed closure of the base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class DeleteCategoryEndpoint : DeleteCategoryEndpointBase
{
    /// <inheritdoc />
    public DeleteCategoryEndpoint(
        CategoryConfigurationProvider categories,
        DataSetConfigurationProvider dataSets,
        IGlossaryTermConfigurationProvider terms,
        ILogger<DeleteCategoryEndpoint> logger)
        : base(logger, categories, dataSets, terms)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Categories");
    }
}
