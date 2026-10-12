using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Identity;
using Fdw.Services.Identity.Endpoints;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to query managed identities by filter, order, page and field: <c>POST /identities/query</c>.
/// </summary>
/// <remarks>
/// The fields a query may name are stated here, where the endpoint is registered, because what this API exposes of
/// an identity is this application's decision. They are the fields the identities list shows, each with the column
/// it reaches and the type its values have; <c>mechanism</c> is the identity's implementation. Deliberately absent:
/// the row's tenancy and visibility columns, its <c>domain</c> name, and everything that lives on the identity's
/// implementation (its credentials and token settings among them) -- none of it can be filtered, sorted on or
/// asked for.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class QueryIdentitiesEndpoint : QueryIdentitiesEndpointBase
{
    private static readonly IReadOnlyList<QueryableField> Fields =
    [
        new("id", "Id", typeof(Guid)),
        new("name", "Name", typeof(string)),
        new("mechanism", "Implementation", typeof(string)),
    ];

    /// <summary>Initializes a new instance of the <see cref="QueryIdentitiesEndpoint"/> class.</summary>
    /// <param name="logger">The logger for this endpoint.</param>
    /// <param name="identities">Reads the page from the identities domain table.</param>
    public QueryIdentitiesEndpoint(ILogger<QueryIdentitiesEndpoint> logger, IdentityServiceConfigurationProvider identities)
        : base(logger ?? NullLogger<QueryIdentitiesEndpoint>.Instance, identities)
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<QueryableField> QueryableFields => Fields;
}
