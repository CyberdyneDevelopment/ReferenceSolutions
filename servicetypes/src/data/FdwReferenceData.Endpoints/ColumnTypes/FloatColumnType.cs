using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server FLOAT column type (also written DOUBLE).</summary>
[TypeOption(typeof(SqlColumnTypes), "Float")]
public sealed class FloatColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="FloatColumnType"/> class.</summary>
    public FloatColumnType()
        : base(10, "Float", SqlDbType.Float, "DOUBLE")
    {
    }
}
