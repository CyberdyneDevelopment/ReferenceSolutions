using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Workspaces.Endpoints;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Endpoint to query data views by filter, order, page and field: <c>POST /data-views/query</c>.</summary>
/// <remarks>
/// The fields a query may name are stated here, where the endpoint is registered, because what this API
/// exposes of a data view is this application's decision. They are the fields the data views list shows,
/// each with the column it reaches and the type its values have. Deliberately absent: the row's tenancy and
/// visibility columns, its <c>domain</c> name, and everything that lives on the view's implementation (its
/// encoding and chart type) -- none of it can be filtered, sorted on or asked for. <c>subjectDataSetId</c> is the
/// view's root data set and <c>parentId</c> the view it is a view of (null for a view of the data set itself).
/// </remarks>
[ExcludeFromCodeCoverage]
public class QueryDataViewsEndpoint : QueryDataViewsEndpointBase
{
    private static readonly IReadOnlyList<QueryableField> Fields =
    [
        new("id", "Id", typeof(Guid)),
        new("name", "Name", typeof(string)),
        new("displayName", "DisplayName", typeof(string)),
        new("description", "Description", typeof(string)),
        new("kind", "Implementation", typeof(string)),
        new("subjectDataSetId", "BaseDataSetId", typeof(Guid)),
        new("parentId", "ParentVariationId", typeof(Guid)),
        new("ownerUserId", "OwnerUserId", typeof(Guid)),
        new("createdAt", "CreatedAt", typeof(DateTimeOffset)),
    ];

    /// <inheritdoc />
    public QueryDataViewsEndpoint(IDataSetVariationConfigurationProvider variations, ILogger<QueryDataViewsEndpoint> logger)
        : base(logger ?? NullLogger<QueryDataViewsEndpoint>.Instance, variations)
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<QueryableField> QueryableFields => Fields;
}
