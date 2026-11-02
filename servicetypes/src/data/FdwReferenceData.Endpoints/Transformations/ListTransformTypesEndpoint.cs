using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceTransformations.Endpoints;

/// <summary>
/// Endpoint to list the available field-transform types.
/// Sealed closure of the generic base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListTransformTypesEndpoint : ListTransformTypesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListTransformTypesEndpoint"/> class.</summary>
    public ListTransformTypesEndpoint(ILogger<ListTransformTypesEndpoint> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
