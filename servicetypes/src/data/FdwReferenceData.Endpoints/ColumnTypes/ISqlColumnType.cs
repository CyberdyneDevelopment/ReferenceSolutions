using System.Collections.Generic;
using System.Data;
using Fdw.Collections;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>
/// A SQL Server column type a DDL request can name (FDW-4153). The request states the option's <c>Name</c>, or one of its
/// <see cref="Spellings"/>; nothing parses an enum to find it.
/// </summary>
public interface ISqlColumnType : ITypeOption<int, ISqlColumnType>
{
    /// <summary>Gets the <see cref="SqlDbType"/> this option is, used only where the column is defined.</summary>
    SqlDbType DbType { get; }

    /// <summary>Gets the other names a request may write this type as (for example INTEGER for Int).</summary>
    IReadOnlyList<string> Spellings { get; }
}
