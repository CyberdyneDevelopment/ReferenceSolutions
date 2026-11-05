using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server DATE column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "Date")]
public sealed class DateColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DateColumnType"/> class.</summary>
    public DateColumnType()
        : base(5, "Date", SqlDbType.Date)
    {
    }
}
