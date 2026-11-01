using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to query data stores by filter, order, page and field: <c>POST /datastores/query</c>.
/// </summary>
/// <remarks>
/// The fields a query may name are stated here, where the endpoint is registered, because what this API
/// exposes of a data store is this application's decision. They are the fields the data stores list shows,
/// each with the column it reaches and the type its values have; <c>storeType</c> is the store's
/// implementation. Deliberately absent: the row's tenancy and visibility columns, its <c>domain</c> name, the
/// connection's NAME (another domain's: the list composes it for callers who may see connections), and
/// everything that lives on the store's implementation (its paths, containers and fields among them) --
/// none of it can be filtered, sorted on or asked for.
/// </remarks>
[ExcludeFromCodeCoverage]
public class QueryDataStoresEndpoint : QueryDataStoresEndpointBase
{
    private static readonly IReadOnlyList<QueryableField> Fields =
    [
        new("id", "Id", typeof(Guid)),
        new("name", "Name", typeof(string)),
        new("storeType", "Implementation", typeof(string)),
        new("connectionId", "ConnectionId", typeof(Guid)),
        new("description", "Description", typeof(string)),
        new("classificationId", "ClassificationId", typeof(int)),
        new("lastDiscoveredAt", "LastDiscoveredAt", typeof(DateTimeOffset)),
        new("createdAt", "CreatedAt", typeof(DateTimeOffset)),
    ];

    /// <inheritdoc />
    public QueryDataStoresEndpoint(DataStoreConfigurationProvider dataStoreProvider, ILogger<QueryDataStoresEndpoint> logger)
        : base(logger ?? NullLogger<QueryDataStoresEndpoint>.Instance, dataStoreProvider)
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<QueryableField> QueryableFields => Fields;
}
