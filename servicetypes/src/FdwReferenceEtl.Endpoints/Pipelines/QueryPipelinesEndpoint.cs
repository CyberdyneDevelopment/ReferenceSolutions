using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Pipelines;
using Fdw.Services.Pipelines.Endpoints;
using Fdw.Web.RestEndpoints.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to query pipelines by filter, order, page and field: <c>POST /pipelines/query</c>.
/// </summary>
/// <remarks>
/// The fields a query may name are stated here, where the endpoint is registered, because what this API
/// exposes of a pipeline is this application's decision. They are the fields the pipelines list shows, each
/// with the column it reaches and the type its values have; <c>pipelineType</c> is the pipeline's
/// implementation. Deliberately absent: the row's tenancy and visibility columns, its <c>domain</c> name and
/// everything that lives on the pipeline's implementation (its stages among them) -- none of it can be
/// filtered, sorted on or asked for.
/// </remarks>
[ExcludeFromCodeCoverage]
public class QueryPipelinesEndpoint : QueryPipelinesEndpointBase
{
    private static readonly IReadOnlyList<QueryableField> Fields =
    [
        new("id", "Id", typeof(Guid)),
        new("name", "Name", typeof(string)),
        new("pipelineType", "Implementation", typeof(string)),
    ];

    /// <inheritdoc />
    public QueryPipelinesEndpoint(PipelineServiceConfigurationProvider pipelineProvider, ILogger<QueryPipelinesEndpoint> logger)
        : base(logger ?? NullLogger<QueryPipelinesEndpoint>.Instance, pipelineProvider)
    {
    }

    /// <inheritdoc />
    protected override IReadOnlyList<QueryableField> QueryableFields => Fields;
}
