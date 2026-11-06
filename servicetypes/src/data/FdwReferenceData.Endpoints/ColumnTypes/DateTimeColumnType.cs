using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server DATETIME column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "DateTime")]
public sealed class DateTimeColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeColumnType"/> class.</summary>
    public DateTimeColumnType()
        : base(6, "DateTime", SqlDbType.DateTime)
    {
    }
}
