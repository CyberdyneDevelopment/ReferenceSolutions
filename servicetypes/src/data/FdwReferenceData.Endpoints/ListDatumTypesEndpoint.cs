using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to list the datum types (FDW-988).
/// Sealed closure of the generic base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListDatumTypesEndpoint : ListDatumTypesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListDatumTypesEndpoint"/> class.</summary>
    public ListDatumTypesEndpoint(ILogger<ListDatumTypesEndpoint> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DatumTypes");
    }
}
