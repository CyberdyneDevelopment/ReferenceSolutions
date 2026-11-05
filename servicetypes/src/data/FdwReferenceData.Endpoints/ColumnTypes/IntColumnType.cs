using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server INT column type (also written INTEGER).</summary>
[TypeOption(typeof(SqlColumnTypes), "Int")]
public sealed class IntColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="IntColumnType"/> class.</summary>
    public IntColumnType()
        : base(12, "Int", SqlDbType.Int, "INTEGER")
    {
    }
}
