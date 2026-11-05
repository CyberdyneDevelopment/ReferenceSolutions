using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server TIMESTAMP column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "Timestamp")]
public sealed class TimestampColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TimestampColumnType"/> class.</summary>
    public TimestampColumnType()
        : base(23, "Timestamp", SqlDbType.Timestamp)
    {
    }
}
