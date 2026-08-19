using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;

namespace ReferenceTransformations.Endpoints;

/// <summary>
/// Endpoint to list the available field-transform types.
/// Sealed closure of the generic base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListTransformTypesEndpoint : ListTransformTypesEndpointBase
{
    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
