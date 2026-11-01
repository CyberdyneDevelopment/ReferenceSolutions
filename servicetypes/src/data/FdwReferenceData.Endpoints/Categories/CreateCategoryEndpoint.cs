using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to create a category.
/// Sealed closure of the base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class CreateCategoryEndpoint : CreateCategoryEndpointBase
{
    /// <inheritdoc />
    public CreateCategoryEndpoint(CategoryConfigurationProvider categories, ITenantContext tenant, ILogger<CreateCategoryEndpoint> logger)
        : base(logger, categories, tenant)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Categories");
    }
}
