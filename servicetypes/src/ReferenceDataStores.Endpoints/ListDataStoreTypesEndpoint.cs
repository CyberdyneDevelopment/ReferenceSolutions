using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;

namespace ReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to list the available DataStore types.
/// Sealed closure of the generic base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListDataStoreTypesEndpoint : ListDataStoreTypesEndpointBase
{
    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
