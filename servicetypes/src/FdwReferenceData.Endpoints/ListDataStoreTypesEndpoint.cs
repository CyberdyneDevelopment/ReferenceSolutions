using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to list the available DataStore types.
/// Sealed closure of the generic base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListDataStoreTypesEndpoint : ListDataStoreTypesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListDataStoreTypesEndpoint"/> class.</summary>
    public ListDataStoreTypesEndpoint(ILogger<ListDataStoreTypesEndpoint> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
