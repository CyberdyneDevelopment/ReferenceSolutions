using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to discover a DataStore's schema and save it.
/// </summary>
/// <remarks>
/// Behaviour lives in the base (FDW-920): it reads the schema through the connection type's discovery
/// and saves what the store lacks. This used to override it with a count of the store's existing
/// paths, which discovered and saved nothing.
/// </remarks>
[ExcludeFromCodeCoverage]
public class DiscoverDataStoreEndpoint : DiscoverDataStoreEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="DiscoverDataStoreEndpoint"/> class.</summary>
    /// <param name="dataStores">Reads and saves the store.</param>
    /// <param name="schema">Discovers through the store's connection and adds what the store lacks.</param>
    /// <param name="logger">The logger.</param>
    public DiscoverDataStoreEndpoint(
        DataStoreConfigurationProvider dataStores,
        ISchemaInformationService schema,
        ILogger<DiscoverDataStoreEndpoint> logger)
        : base(dataStores, schema, logger)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("DataStores");
    }
}
