using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server DATETIMEOFFSET column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "DateTimeOffset")]
public sealed class DateTimeOffsetColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTimeOffsetColumnType"/> class.</summary>
    public DateTimeOffsetColumnType()
        : base(8, "DateTimeOffset", SqlDbType.DateTimeOffset)
    {
    }
}
