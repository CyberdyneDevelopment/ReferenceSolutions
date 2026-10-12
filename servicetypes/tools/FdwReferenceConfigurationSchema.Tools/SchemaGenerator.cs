using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace FdwReferenceConfigurationSchema.Tools;

/// <summary>
/// Introspects a live ConfigurationDb's schema and builds the DataStores/Paths block of
/// configurationSchema.json — everything read from sys.columns / sys.key_constraints /
/// sys.foreign_keys. Connections, SecretManagers, and ServerConfiguration stay hand-maintained
/// in the target app's configurationSchema.json; this generator only emits the Paths array.
/// </summary>
/// <remarks>
/// Ported from raydar-seed-data/tools/generate-configuration-schema.ps1 so it runs without
/// PowerShell, the SqlServer module, or the Azure CLI. Key rules carried over unchanged (from
/// FDW's DefaultConfigurationProvider read path):
/// <list type="bullet">
/// <item>Physical key "PK_&lt;table&gt;" on the PK column (RowId).</item>
/// <item>Logical key on Id (no ReferencedContainerName) when an Id column exists.</item>
/// <item>Foreign key only when FkColumn == RefTable + "RowId" (a structural parent; excludes
/// incidental refs like AssociatedDataStoreRowId and the cross-cutting TenantRowId /
/// VisibilityGroupRowId).</item>
/// <item>IsSystemProvided = is_identity OR is_computed OR has-default.</item>
/// <item>Field Name is the real column name from sys.columns, never hardcoded — a renamed column
/// (e.g. ServiceOptionType to Implementation) is picked up automatically.</item>
/// </list>
/// </remarks>
public static class SchemaGenerator
{
    /// <summary>The full set of schemas the FDW configuration providers read. dbo is excluded — synonyms/infra only.</summary>
    public static readonly IReadOnlyList<string> Schemas =
    [
        "agent", "audit", "auth", "authz", "calc", "catalog", "conn", "cred", "crs", "data",
        "workspace", "dg", "hlth", "hst", "log", "msg", "notify", "otel", "pipe", "quality",
        "rsl", "sched", "sec", "security", "settings", "tenant", "transform", "usr", "workflow",
    ];

    private static readonly HashSet<string> IgnoredForeignKeyColumns = ["TenantRowId", "VisibilityGroupRowId"];

    /// <summary>Builds the Paths array by querying the given open connection.</summary>
    public static async Task<List<SchemaPath>> Generate(SqlConnection connection, CancellationToken cancellationToken)
    {
        var columnsByTable = await ReadColumns(connection, cancellationToken);
        var primaryKeysByTable = await ReadPrimaryKeys(connection, cancellationToken);
        var foreignKeysByTable = await ReadForeignKeys(connection, cancellationToken);

        var paths = new List<SchemaPath>();
        foreach (var schema in Schemas)
        {
            var tablesInSchema = new SortedSet<string>();
            foreach (var key in columnsByTable.Keys)
            {
                if (key.Schema == schema)
                {
                    tablesInSchema.Add(key.Table);
                }
            }

            if (tablesInSchema.Count == 0)
            {
                continue;
            }

            var containers = new List<SchemaContainer>();
            foreach (var table in tablesInSchema)
            {
                var tableKey = new TableKey(schema, table);
                containers.Add(BuildContainer(table, columnsByTable[tableKey], primaryKeysByTable, foreignKeysByTable, tableKey));
            }

            paths.Add(new SchemaPath { Name = schema, PathType = "DatabasePath", Containers = containers });
        }

        return paths;
    }

    private static SchemaContainer BuildContainer(
        string table,
        List<ColumnRow> columns,
        Dictionary<TableKey, List<string>> primaryKeysByTable,
        Dictionary<TableKey, List<ForeignKeyRow>> foreignKeysByTable,
        TableKey tableKey)
    {
        var fields = new List<SchemaField>();
        var fieldNames = new HashSet<string>();
        foreach (var column in columns)
        {
            fieldNames.Add(column.Name);
            fields.Add(new SchemaField
            {
                Name = column.Name,
                Ordinal = column.Ordinal,
                DataType = column.SqlType,
                IsNullable = column.IsNullable,
                IsSystemProvided = column.IsSystem,
                HasDefault = column.IsSystem,
                Visible = true,
            });
        }

        var keys = new List<SchemaKey>();
        if (primaryKeysByTable.TryGetValue(tableKey, out var pkColumns))
        {
            var keyFields = new List<SchemaKeyField>();
            foreach (var pkColumn in pkColumns)
            {
                keyFields.Add(new SchemaKeyField { Name = pkColumn, Ordinal = 0 });
            }

            keys.Add(new SchemaKey { Name = $"PK_{table}", TypeId = "Physical", KeyFields = keyFields });
        }

        if (fieldNames.Contains("Id"))
        {
            keys.Add(new SchemaKey
            {
                Name = $"LK_{table}_Id",
                TypeId = "Logical",
                KeyFields = [new SchemaKeyField { Name = "Id", Ordinal = 0 }],
            });
        }

        if (foreignKeysByTable.TryGetValue(tableKey, out var foreignKeys))
        {
            foreach (var fk in foreignKeys)
            {
                if (IgnoredForeignKeyColumns.Contains(fk.ForeignKeyColumn))
                {
                    continue;
                }

                if (fk.ForeignKeyColumn != fk.ReferencedTable + "RowId")
                {
                    continue;
                }

                keys.Add(new SchemaKey
                {
                    Name = $"FK_{table}_{fk.ReferencedTable}",
                    TypeId = "Foreign",
                    ReferencedContainerName = fk.ReferencedTable,
                    ReferencedKeyName = $"PK_{fk.ReferencedTable}",
                    KeyFields = [new SchemaKeyField { Name = fk.ForeignKeyColumn, Ordinal = 0 }],
                });
            }
        }

        return new SchemaContainer { Name = table, ContainerType = "Table", Format = "Table", Fields = fields, Keys = keys };
    }

    private static async Task<Dictionary<TableKey, List<ColumnRow>>> ReadColumns(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.name AS sch, t.name AS tbl, c.name AS col, c.column_id AS ord,
                   ty.name AS sqltype, c.is_nullable AS is_nullable,
                   CASE WHEN c.is_identity = 1 OR c.is_computed = 1 OR c.default_object_id <> 0 THEN 1 ELSE 0 END AS is_system
            FROM sys.columns c
            JOIN sys.tables t ON t.object_id = c.object_id
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            JOIN sys.types ty ON ty.user_type_id = c.user_type_id
            WHERE s.name IN ({0})
            ORDER BY s.name, t.name, c.column_id;
            """;

        var result = new Dictionary<TableKey, List<ColumnRow>>();
        await using var command = new SqlCommand(FormatSchemaFilter(sql), connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var key = new TableKey(reader.GetString(0), reader.GetString(1));
            if (!result.TryGetValue(key, out var list))
            {
                list = [];
                result[key] = list;
            }

            list.Add(new ColumnRow(
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetString(4),
                reader.GetBoolean(5),
                reader.GetInt32(6) == 1));
        }

        return result;
    }

    private static async Task<Dictionary<TableKey, List<string>>> ReadPrimaryKeys(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT s.name AS sch, t.name AS tbl, c.name AS col
            FROM sys.key_constraints kc
            JOIN sys.tables t ON t.object_id = kc.parent_object_id
            JOIN sys.schemas s ON s.schema_id = t.schema_id
            JOIN sys.index_columns ic ON ic.object_id = kc.parent_object_id AND ic.index_id = kc.unique_index_id
            JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE kc.type = 'PK' AND s.name IN ({0})
            ORDER BY s.name, t.name, ic.key_ordinal;
            """;

        var result = new Dictionary<TableKey, List<string>>();
        await using var command = new SqlCommand(FormatSchemaFilter(sql), connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var key = new TableKey(reader.GetString(0), reader.GetString(1));
            if (!result.TryGetValue(key, out var list))
            {
                list = [];
                result[key] = list;
            }

            list.Add(reader.GetString(2));
        }

        return result;
    }

    private static async Task<Dictionary<TableKey, List<ForeignKeyRow>>> ReadForeignKeys(SqlConnection connection, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT ps.name AS sch, pt.name AS tbl, pc.name AS fkcol, rt.name AS reftbl, rc.name AS refcol
            FROM sys.foreign_keys fk
            JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
            JOIN sys.tables pt ON pt.object_id = fk.parent_object_id
            JOIN sys.schemas ps ON ps.schema_id = pt.schema_id
            JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
            JOIN sys.tables rt ON rt.object_id = fk.referenced_object_id
            JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id
            WHERE ps.name IN ({0})
            ORDER BY ps.name, pt.name;
            """;

        var result = new Dictionary<TableKey, List<ForeignKeyRow>>();
        await using var command = new SqlCommand(FormatSchemaFilter(sql), connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var key = new TableKey(reader.GetString(0), reader.GetString(1));
            if (!result.TryGetValue(key, out var list))
            {
                list = [];
                result[key] = list;
            }

            list.Add(new ForeignKeyRow(reader.GetString(2), reader.GetString(3)));
        }

        return result;
    }

    private static string FormatSchemaFilter(string sqlWithPlaceholder)
    {
        var quoted = new List<string>(Schemas.Count);
        foreach (var schema in Schemas)
        {
            quoted.Add($"'{schema}'");
        }

        return string.Format(CultureInfo.InvariantCulture, sqlWithPlaceholder, string.Join(",", quoted));
    }

    private readonly record struct TableKey(string Schema, string Table);

    private sealed record ColumnRow(string Name, int Ordinal, string SqlType, bool IsNullable, bool IsSystem);

    private sealed record ForeignKeyRow(string ForeignKeyColumn, string ReferencedTable);
}
