using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server MONEY column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "Money")]
public sealed class MoneyColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="MoneyColumnType"/> class.</summary>
    public MoneyColumnType()
        : base(13, "Money", SqlDbType.Money)
    {
    }
}
