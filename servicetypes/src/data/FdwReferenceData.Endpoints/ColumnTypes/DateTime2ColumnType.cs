using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server DATETIME2 column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "DateTime2")]
public sealed class DateTime2ColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DateTime2ColumnType"/> class.</summary>
    public DateTime2ColumnType()
        : base(7, "DateTime2", SqlDbType.DateTime2)
    {
    }
}
