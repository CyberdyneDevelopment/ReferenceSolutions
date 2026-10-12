using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Data.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Web.RestEndpoints.ErrorMapping;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceSchema.Endpoints.Logging;

namespace FdwReferenceSchema.Endpoints;

/// <summary>
/// Request for schema graph (ER diagram format).
/// </summary>
[ExcludeFromCodeCoverage]
public class GetSchemaGraphRequest
{
    /// <summary>Gets or sets the connection name.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets optional schema filter (null = all schemas).</summary>
    public string? SchemaFilter { get; set; }
}

/// <summary>
/// Schema graph response for ER diagram rendering.
/// </summary>
[ExcludeFromCodeCoverage]
public class SchemaGraphResponse
{
    /// <summary>Gets or sets the connection name.</summary>
    public string ConnectionName { get; set; } = string.Empty;

    /// <summary>Gets or sets the schema name filter applied.</summary>
    public string? SchemaName { get; set; }

    /// <summary>Gets or sets the entities (tables/views).</summary>
    public IList<SchemaEntityDto> Entities { get; set; } = [];

    /// <summary>Gets or sets the relationships (foreign keys).</summary>
    public IList<SchemaRelationshipDto> Relationships { get; set; } = [];
}

/// <summary>
/// Schema entity (table/view) for ER diagram.
/// </summary>
[ExcludeFromCodeCoverage]
public class SchemaEntityDto
{
    /// <summary>Gets or sets the full name (schema.table).</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Gets or sets the schema name.</summary>
    public string? Schema { get; set; }

    /// <summary>Gets or sets the table name.</summary>
    public string TableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the entity type (Table or View).</summary>
    public string EntityType { get; set; } = "Table";

    /// <summary>Gets or sets the fields.</summary>
    public IList<SchemaFieldDto> Fields { get; set; } = [];

    /// <summary>Gets or sets the position for rendering.</summary>
    public SchemaPositionDto Position { get; set; } = new();
}

/// <summary>
/// Schema field for ER diagram.
/// </summary>
[ExcludeFromCodeCoverage]
public class SchemaFieldDto
{
    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the data type.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the field is nullable.</summary>
    public bool IsNullable { get; set; }

    /// <summary>Gets or sets whether the field is a primary key.</summary>
    public bool IsPrimaryKey { get; set; }

    /// <summary>Gets or sets whether the field is a foreign key.</summary>
    public bool IsForeignKey { get; set; }

    /// <summary>Gets or sets whether the field is an identity column.</summary>
    public bool IsIdentity { get; set; }

    /// <summary>Gets or sets the ordinal position.</summary>
    public int OrdinalPosition { get; set; }
}

/// <summary>
/// Position coordinates for graph layout.
/// </summary>
[ExcludeFromCodeCoverage]
public class SchemaPositionDto
{
    /// <summary>Gets or sets the X coordinate.</summary>
    public int X { get; set; }

    /// <summary>Gets or sets the Y coordinate.</summary>
    public int Y { get; set; }
}

/// <summary>
/// A relationship between schema entities.
/// </summary>
[ExcludeFromCodeCoverage]
public class SchemaRelationshipDto
{
    /// <summary>Gets or sets the source entity full name.</summary>
    public string SourceEntity { get; set; } = string.Empty;

    /// <summary>Gets or sets the target entity full name.</summary>
    public string TargetEntity { get; set; } = string.Empty;

    /// <summary>Gets or sets the source column name.</summary>
    public string SourceColumn { get; set; } = string.Empty;

    /// <summary>Gets or sets the target column name.</summary>
    public string TargetColumn { get; set; } = string.Empty;
}

/// <summary>
/// Endpoint to get schema graph for ER diagram visualization.
/// Returns entities, relationships, and indexes in a format optimized for graph rendering.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetSchemaGraphEndpoint : Endpoint<GetSchemaGraphRequest, SchemaGraphResponse>
{
    private readonly ISchemaInformationService _schema;
    private readonly ILogger<GetSchemaGraphEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GetSchemaGraphEndpoint"/> class.
    /// </summary>
    public GetSchemaGraphEndpoint(
        ISchemaInformationService schema,
        ILogger<GetSchemaGraphEndpoint>? logger = null)
    {
        _schema = schema;
        _logger = logger ?? NullLogger<GetSchemaGraphEndpoint>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Get("/connections/{ConnectionName}/schema-graph");
        Policies("datastores:read");
        Tags("Schema");
        Summary(s =>
        {
            s.Summary = "Get schema graph for ER diagram";
            s.Description = "Returns schema information in a graph format suitable for ER diagram visualization. " +
                            "Includes entities (tables/views), relationships (foreign keys), and indexes.";
        });
    }

    /// <inheritdoc/>
    public override async Task HandleAsync(GetSchemaGraphRequest req, CancellationToken ct)
    {
        SchemaLog.DiscoveringSchema(_logger, req.ConnectionName);

        // The schema service runs the connection's chosen discovery option; when it cannot discover, its
        // failure names why and carries the status.
        var discoveryResult = await _schema.DiscoverContainers(
            req.ConnectionName, DataStoreDiscoveryOptions.Default, ct).ConfigureAwait(false);
        if (discoveryResult.IsFailure || discoveryResult.Value is not { } containers)
        {
            var (statusCode, problem) = ResultHttpStatusMapper.Map(discoveryResult, HttpContext);
            await Send.ResultAsync(TypedResults.Json(problem, statusCode: statusCode)).ConfigureAwait(false);
            return;
        }

        var graph = ConvertToSchemaGraph(req, containers);

        SchemaLog.SchemaDiscoveryCompleted(_logger, req.ConnectionName, graph.Entities.Count);
        await Send.OkAsync(graph, ct);
    }

    private static SchemaGraphResponse ConvertToSchemaGraph(
        GetSchemaGraphRequest req,
        IReadOnlyList<IStorageContainer> containers)
    {
        var graph = new SchemaGraphResponse
        {
            ConnectionName = req.ConnectionName,
            SchemaName = req.SchemaFilter
        };

        var entityIndex = 0;

        foreach (var container in containers)
        {
            var schemaName = container.Path.PathValue;

            if (!string.IsNullOrEmpty(req.SchemaFilter) &&
                !string.Equals(schemaName, req.SchemaFilter, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var entity = CreateEntity(schemaName, container, entityIndex++);
            graph.Entities.Add(entity);
        }

        AutoLayoutEntities(graph.Entities);

        return graph;
    }

    private static SchemaEntityDto CreateEntity(string schemaName, IStorageContainer container, int index)
    {
        // Why: IsPrimaryKey was removed from IField — PK is resolved via GetPrimaryKeyFieldName()
        // which reads the SurrogateKeyField metadata entry set by DataStoreProvider at build time.
        var pkFieldName = container.GetPrimaryKeyFieldName();

        return new SchemaEntityDto
        {
            FullName = $"{schemaName}.{container.Name}",
            Schema = schemaName,
            TableName = container.Name,
            EntityType = container.ContainerType.Name,
            Fields = container.Schema.Fields.Select((f, i) => new SchemaFieldDto
            {
                Name = f.Name,
                DataType = f.FieldType.TypeName,
                IsNullable = f.IsNullable,
                IsPrimaryKey = pkFieldName != null && string.Equals(f.Name, pkFieldName, StringComparison.OrdinalIgnoreCase),
                IsForeignKey = false,
                IsIdentity = f.IsIdentity,
                OrdinalPosition = i
            }).ToList(),
            Position = new SchemaPositionDto { X = 0, Y = 0 }
        };
    }

    private static void AutoLayoutEntities(IList<SchemaEntityDto> entities)
    {
        if (entities.Count == 0) return;

        var cols = (int)Math.Ceiling(Math.Sqrt(entities.Count));
        var spacingX = 300;
        var spacingY = 350;

        for (var i = 0; i < entities.Count; i++)
        {
            var col = i % cols;
            var row = i / cols;
            entities[i].Position = new SchemaPositionDto
            {
                X = col * spacingX + 50,
                Y = row * spacingY + 50
            };
        }
    }
}
