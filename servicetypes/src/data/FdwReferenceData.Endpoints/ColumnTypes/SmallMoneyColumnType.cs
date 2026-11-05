using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server SMALLMONEY column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "SmallMoney")]
public sealed class SmallMoneyColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SmallMoneyColumnType"/> class.</summary>
    public SmallMoneyColumnType()
        : base(20, "SmallMoney", SqlDbType.SmallMoney)
    {
    }
}
