using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server CHAR column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "Char")]
public sealed class CharColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="CharColumnType"/> class.</summary>
    public CharColumnType()
        : base(4, "Char", SqlDbType.Char)
    {
    }
}
