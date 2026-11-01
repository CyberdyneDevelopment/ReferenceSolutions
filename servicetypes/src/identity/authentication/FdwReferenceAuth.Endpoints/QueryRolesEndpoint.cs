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
/// Endpoint to query roles by filter, order, page and field: <c>POST /roles/query</c>.
/// </summary>
/// <remarks>
/// The fields a query may name are stated here, where the endpoint is registered, because what this API
/// exposes of a role is this application's decision. They are the fields the roles list shows, each with the
/// column it reaches and the type its values have. Deliberately absent: the row's tenancy and visibility
/// columns, its <c>domain</c> and <c>implementation</c> names, and everything that lives on the role's
/// implementation rather than on the roles domain row (its permissions among them) -- none of it can be
/// filtered, sorted on or asked for.
/// </remarks>
[ExcludeFromCodeCoverage]
public class QueryRolesEndpoint : QueryRolesEndpointBase
{
    private static readonly IReadOnlyList<QueryableField> Fields =
    [
        new("id", "Id", typeof(Guid)),
        new("name", "Name", typeof(string)),
        new("displayName", "DisplayName", typeof(string)),
        new("description", "Description", typeof(string)),
        new("isTenantScoped", "IsTenantScoped", typeof(bool)),
        new("createdAt", "CreatedAt", typeof(DateTimeOffset)),
    ];

    /// <inheritdoc />
    public QueryRolesEndpoint(IRoleConfigurationProvider roleProvider, ILogger<QueryRolesEndpoint> logger)
        : base(logger ?? NullLogger<QueryRolesEndpoint>.Instance, roleProvider)
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<QueryableField> QueryableFields => Fields;
}
