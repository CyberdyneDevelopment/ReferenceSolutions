using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server TINYINT column type (also written BYTE).</summary>
[TypeOption(typeof(SqlColumnTypes), "TinyInt")]
public sealed class TinyIntColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TinyIntColumnType"/> class.</summary>
    public TinyIntColumnType()
        : base(24, "TinyInt", SqlDbType.TinyInt, "BYTE")
    {
    }
}
