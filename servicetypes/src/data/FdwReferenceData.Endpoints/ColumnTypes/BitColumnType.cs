using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server BIT column type (also written BOOLEAN, BOOL).</summary>
[TypeOption(typeof(SqlColumnTypes), "Bit")]
public sealed class BitColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="BitColumnType"/> class.</summary>
    public BitColumnType()
        : base(3, "Bit", SqlDbType.Bit, "BOOLEAN", "BOOL")
    {
    }
}
