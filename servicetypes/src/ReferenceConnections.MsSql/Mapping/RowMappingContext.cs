using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Fdw.Data.Abstractions;
using Fdw.Data.MsSql;
using Microsoft.Data.SqlClient;

using Fdw.Services.Connections.MsSql;

namespace ReferenceConnections.MsSql.Mapping;

/// <summary>
/// Pre-computed mapping context for efficient row-to-dictionary conversion.
/// Caches field ordinals and converters ONCE per read operation to avoid
/// per-row allocations and lookups.
/// </summary>
[ExcludeFromCodeCoverage] // Excluded: requires SQL Server connection
internal sealed class RowMappingContext
{
    /// <summary>
    /// Pre-computed ordinals for each field. -1 indicates field not found in result set.
    /// </summary>
    public readonly int[] FieldOrdinals;

    /// <summary>
    /// Pre-looked-up converters for each field. Null if no converter specified.
    /// </summary>
    public readonly IDataTypeConverter?[] Converters;

    /// <summary>
    /// Field names matching the ordinal/converter arrays.
    /// </summary>
    public readonly string[] FieldNames;

    /// <summary>
    /// Number of fields to process.
    /// </summary>
    public readonly int FieldCount;

    private RowMappingContext(int[] ordinals, IDataTypeConverter?[] converters, string[] names)
    {
        FieldOrdinals = ordinals;
        Converters = converters;
        FieldNames = names;
        FieldCount = names.Length;
    }

    /// <summary>
    /// Creates a mapping context from a reader and container schema.
    /// This method should be called ONCE before reading any rows.
    /// </summary>
    /// <param name="reader">The SqlDataReader to read from.</param>
    /// <param name="container">The container with schema metadata.</param>
    /// <returns>A pre-computed mapping context.</returns>
    public static RowMappingContext Create(SqlDataReader reader, IStorageContainer container)
    {
        // Handle null container or schema - return empty context
        if (container?.Schema?.Fields == null || container.Schema.Fields.Count == 0)
        {
            return new RowMappingContext([], [], []);
        }

        // Cache converter lookup ONCE (not per row!)
        var converterLookup = MsSqlConverters.All().ToDictionary(c => c.Id);

        var fields = container.Schema.Fields;
        var count = fields.Count;
        var ordinals = new int[count];
        var converters = new IDataTypeConverter?[count];
        var names = new string[count];

        for (int i = 0; i < count; i++)
        {
            var field = fields[i];
            names[i] = field.Name;

            // Pre-compute ordinal (O(n) once vs O(n*m) per row)
            try
            {
                ordinals[i] = reader.GetOrdinal(field.Name);
            }
            catch (IndexOutOfRangeException ex)
            {
                // Why: GetOrdinal throws IndexOutOfRangeException when the column is absent from the
                // result set. Ordinal -1 signals "field missing" so the row mapper skips it cleanly.
                // The exception is observed via ex; no logger is available in this static factory.
                _ = ex;
                ordinals[i] = -1;
            }

            // Pre-lookup converter (O(1) dictionary lookup vs O(n) linear search per row)
            if (field.ConverterTypeId.HasValue &&
                converterLookup.TryGetValue(field.ConverterTypeId.Value, out var conv))
            {
                converters[i] = conv;
            }
        }

        return new RowMappingContext(ordinals, converters, names);
    }
}
