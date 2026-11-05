using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server BIGINT column type (also written LONG).</summary>
[TypeOption(typeof(SqlColumnTypes), "BigInt")]
public sealed class BigIntColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="BigIntColumnType"/> class.</summary>
    public BigIntColumnType()
        : base(1, "BigInt", SqlDbType.BigInt, "LONG")
    {
    }
}
