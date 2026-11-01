using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to read the category tree.
/// Sealed closure of the base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetCategoryTreeEndpoint : GetCategoryTreeEndpointBase
{
    /// <inheritdoc />
    public GetCategoryTreeEndpoint(CategoryConfigurationProvider categories, ILogger<GetCategoryTreeEndpoint> logger)
        : base(logger, categories)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Categories");
    }
}
