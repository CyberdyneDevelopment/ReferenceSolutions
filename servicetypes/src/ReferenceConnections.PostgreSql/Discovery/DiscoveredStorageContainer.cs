using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Data;
using Fdw.Data.Abstractions;

using Fdw.Services.Connections.PostgreSql;
using Fdw.Services.Connections.PostgreSql.Discovery;

namespace ReferenceConnections.PostgreSql.Discovery;

/// <summary>
/// Storage container created from discovered PostgreSQL schema.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires PostgreSQL connection
public sealed class DiscoveredStorageContainer : IStorageContainer
{
    private readonly IReadOnlyList<string> _primaryKeyColumns;
    private readonly IReadOnlyList<DiscoveredIndex> _indexes;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiscoveredStorageContainer"/> class from a
    /// schema-import result row.
    /// </summary>
    /// <param name="name">The container's object name.</param>
    /// <param name="containerType">The discovered container kind (table, view).</param>
    /// <param name="format">The format the container's rows are read and written in.</param>
    /// <param name="schema">The discovered field schema.</param>
    /// <param name="path">The container's physical address.</param>
    /// <param name="schemaName">The owning schema name, e.g. <c>public</c>.</param>
    /// <param name="primaryKeyColumns">Column names forming the primary key, in key order.</param>
    /// <param name="indexes">Indexes discovered on the container.</param>
    public DiscoveredStorageContainer(
        string name,
        IContainerType containerType,
        IFormatType format,
        IContainerSchema schema,
        IPath path,
        string schemaName,
        IReadOnlyList<string> primaryKeyColumns,
        IReadOnlyList<DiscoveredIndex> indexes)
    {
        Name = name;
        ContainerType = containerType;
        Format = format;
        Schema = schema;
        Path = path;
        SchemaName = schemaName;
        _primaryKeyColumns = primaryKeyColumns;
        _indexes = indexes;

        // Set supported operations based on container type
        SupportedOperations = containerType.Name switch
        {
            "View" => ["Query"],
            "Table" => ["Query", "Insert", "Update", "Delete"],
            _ => ["Query"]
        };

        // Build metadata
        var metadata = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["SchemaName"] = schemaName,
            ["ContainerType"] = containerType.Name,
            ["DiscoveredAt"] = DateTime.UtcNow.ToString("O"),
            ["PrimaryKeyColumns"] = string.Join(",", primaryKeyColumns),
            ["IndexCount"] = indexes.Count
        };
        Metadata = metadata;
    }

    /// <inheritdoc/>
    public string Name { get; }

    /// <inheritdoc/>
    public IContainerType ContainerType { get; }

    /// <inheritdoc/>
    public IFormatType Format { get; }

    /// <inheritdoc/>
    public IContainerSchema Schema { get; }

    /// <inheritdoc/>
    public IPath Path { get; }

    /// <inheritdoc/>
    public string[] SupportedOperations { get; }

    /// <inheritdoc/>
    public IReadOnlyDictionary<string, object> Metadata { get; }

    /// <summary>
    /// Gets the database schema name.
    /// </summary>
    public string SchemaName { get; }

    /// <summary>
    /// Gets the primary key column names.
    /// </summary>
    public IReadOnlyList<string> PrimaryKeyColumns => _primaryKeyColumns;

    /// <summary>
    /// Gets the discovered indexes.
    /// </summary>
    public IReadOnlyList<DiscoveredIndex> Indexes => _indexes;
}
