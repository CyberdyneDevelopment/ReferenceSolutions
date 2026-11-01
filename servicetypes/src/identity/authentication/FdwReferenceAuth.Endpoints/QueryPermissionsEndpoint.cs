using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authorization;
using Fdw.Services.Authorization.Endpoints;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to query permissions by filter, order, page and field: <c>POST /permissions/query</c>.
/// </summary>
/// <remarks>
/// The fields a query may name are stated here, where the endpoint is registered, because what this API
/// exposes of a permission is this application's decision. They are the fields the permissions list shows,
/// each with the column it reaches and the type its values have; <c>domain</c> is the permission's own
/// domain, the <c>PermissionDomain</c> column. Deliberately absent: the row's tenancy and visibility
/// columns and its <c>domain</c> and <c>implementation</c> names as the domain table holds them -- none of
/// it can be filtered, sorted on or asked for.
/// </remarks>
[ExcludeFromCodeCoverage]
public class QueryPermissionsEndpoint : QueryPermissionsEndpointBase
{
    private static readonly IReadOnlyList<QueryableField> Fields =
    [
        new("id", "Id", typeof(Guid)),
        new("name", "Name", typeof(string)),
        new("domain", "PermissionDomain", typeof(string)),
        new("resource", "Resource", typeof(string)),
        new("action", "Action", typeof(string)),
        new("scope", "Scope", typeof(string)),
        new("displayName", "DisplayName", typeof(string)),
        new("description", "Description", typeof(string)),
        new("category", "Category", typeof(string)),
    ];

    /// <inheritdoc />
    public QueryPermissionsEndpoint(IPermissionConfigurationProvider permissionProvider, ILogger<QueryPermissionsEndpoint> logger)
        : base(logger ?? NullLogger<QueryPermissionsEndpoint>.Instance, permissionProvider)
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<QueryableField> QueryableFields => Fields;
}
