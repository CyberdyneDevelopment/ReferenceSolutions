using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to list the types of category.
/// Sealed closure of the base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetCategoryTypesEndpoint : GetCategoryTypesEndpointBase
{
    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Categories");
    }
}
