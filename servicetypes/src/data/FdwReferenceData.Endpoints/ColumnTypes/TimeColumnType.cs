using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server TIME column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "Time")]
public sealed class TimeColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TimeColumnType"/> class.</summary>
    public TimeColumnType()
        : base(22, "Time", SqlDbType.Time)
    {
    }
}
