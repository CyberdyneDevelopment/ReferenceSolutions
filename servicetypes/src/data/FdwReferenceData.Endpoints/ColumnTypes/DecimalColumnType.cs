using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server DECIMAL column type (also written NUMERIC).</summary>
[TypeOption(typeof(SqlColumnTypes), "Decimal")]
public sealed class DecimalColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DecimalColumnType"/> class.</summary>
    public DecimalColumnType()
        : base(9, "Decimal", SqlDbType.Decimal, "NUMERIC")
    {
    }
}
