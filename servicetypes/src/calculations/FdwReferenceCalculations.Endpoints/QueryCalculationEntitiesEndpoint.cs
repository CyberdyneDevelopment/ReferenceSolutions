using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Calculations.Endpoints.CalculationEntities;
using Fdw.Services.Calculations;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceCalculations.Endpoints;

/// <summary>
/// Endpoint to query calculation entities by filter, order, page and field: <c>POST /calculation-entities/query</c>.
/// </summary>
/// <remarks>
/// The fields a query may name are stated here, where the endpoint is registered, because what this API exposes of
/// a calculation is this application's decision. They are the fields the calculations list shows, each with the
/// column it reaches and the type its values have; <c>calculationEntityType</c> is the calculation's
/// implementation. Deliberately absent: the row's tenancy and visibility columns, its <c>domain</c> name, and
/// everything that lives on the calculation's implementation (its formula, inputs and steps among them) -- none
/// of it can be filtered, sorted on or asked for.
/// </remarks>
[ExcludeFromCodeCoverage]
public class QueryCalculationEntitiesEndpoint : QueryCalculationEntitiesEndpointBase
{
    private static readonly IReadOnlyList<QueryableField> Fields =
    [
        new("id", "Id", typeof(Guid)),
        new("name", "Name", typeof(string)),
        new("description", "Description", typeof(string)),
        new("calculationEntityType", "Implementation", typeof(string)),
        new("isEnabled", "IsEnabled", typeof(bool)),
    ];

    /// <summary>
    /// Initializes a new instance of <see cref="QueryCalculationEntitiesEndpoint"/>.
    /// </summary>
    public QueryCalculationEntitiesEndpoint(ILogger<QueryCalculationEntitiesEndpoint> logger, ICalculationConfigurationProvider provider)
        : base(logger ?? NullLogger<QueryCalculationEntitiesEndpoint>.Instance, provider)
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<QueryableField> QueryableFields => Fields;
}
