using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>
/// The SQL Server column types a DDL request can name (FDW-4153). <see cref="Named"/> answers the option for a type's name or one of its
/// spellings, or <c>NotFound</c> for anything else; nothing stands in for an unknown type.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SqlColumnTypeBase), typeof(ISqlColumnType), typeof(SqlColumnTypes))]
public abstract partial class SqlColumnTypes : TypeCollectionBase<SqlColumnTypeBase, ISqlColumnType>
{
    /// <summary>Finds the column type a request names, by its name or one of its spellings.</summary>
    /// <param name="text">The type as the request writes it.</param>
    /// <returns>The option, or <c>NotFound</c> when the text names none.</returns>
    /// <remarks>
    /// Why case is ignored here and not in <c>ByName</c>: the type is typed by a person and SQL type names are case-insensitive in SQL
    /// itself, so INT, int and Int are one type, not three requests.
    /// </remarks>
    public static ISqlColumnType Named(string text)
    {
        var wanted = text.Trim();
        foreach (var type in All())
        {
            if (string.Equals(type.Name, wanted, StringComparison.OrdinalIgnoreCase) || Spelled(type, wanted))
                return type;
        }

        return NotFound;
    }

    private static bool Spelled(ISqlColumnType type, string wanted)
    {
        foreach (var spelling in type.Spellings)
        {
            if (string.Equals(spelling, wanted, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
