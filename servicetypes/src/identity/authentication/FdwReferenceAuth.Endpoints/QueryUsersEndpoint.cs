using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to query users by filter, order, page and field: <c>POST /users/query</c> (Admin only).
/// </summary>
/// <remarks>
/// The fields a query may name are stated here, where the endpoint is registered, because what this API
/// exposes of a user is this application's decision. They are the fields the users list shows, each with the
/// column it reaches and the type its values have. Deliberately absent: the row's tenancy and visibility
/// columns, its <c>domain</c> and <c>implementation</c> names, and everything that lives on the user's
/// account rather than on the users domain row -- none of it can be filtered, sorted on or asked for.
/// </remarks>
[ExcludeFromCodeCoverage]
public class QueryUsersEndpoint : QueryUsersEndpointBase
{
    private static readonly IReadOnlyList<QueryableField> Fields =
    [
        new("id", "Id", typeof(Guid)),
        new("name", "Name", typeof(string)),
        new("email", "Email", typeof(string)),
        new("isActive", "IsActive", typeof(bool)),
        new("createdAt", "CreatedAt", typeof(DateTimeOffset)),
    ];

    /// <inheritdoc />
    public QueryUsersEndpoint(UserConfigurationProvider userProvider, ILogger<QueryUsersEndpoint> logger)
        : base(logger ?? NullLogger<QueryUsersEndpoint>.Instance, userProvider)
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<QueryableField> QueryableFields => Fields;
}
