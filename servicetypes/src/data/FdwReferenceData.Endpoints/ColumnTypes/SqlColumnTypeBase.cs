using System.Collections.Generic;
using System.Data;
using Fdw.Collections;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>
/// Base of the <see cref="SqlColumnTypes"/> options: each names a column type, carries the <see cref="SqlDbType"/> it is and the other
/// names a request may write it as.
/// </summary>
public abstract class SqlColumnTypeBase : TypeOptionBase<SqlColumnTypeBase>, ISqlColumnType
{
    /// <summary>Initializes a new instance of the <see cref="SqlColumnTypeBase"/> class.</summary>
    /// <param name="id">The option's id, unique in the collection.</param>
    /// <param name="name">The type's name, as <see cref="SqlDbType"/> names it.</param>
    /// <param name="dbType">The <see cref="SqlDbType"/> this option is.</param>
    /// <param name="spellings">The other names a request may write it as.</param>
    protected SqlColumnTypeBase(int id, string name, SqlDbType dbType, params string[] spellings)
        : base(id, name)
    {
        DbType = dbType;
        Spellings = spellings;
    }

    /// <inheritdoc />
    public SqlDbType DbType { get; }

    /// <inheritdoc />
    public IReadOnlyList<string> Spellings { get; }
}
