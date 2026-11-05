using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server NTEXT column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "NText")]
public sealed class NTextColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="NTextColumnType"/> class.</summary>
    public NTextColumnType()
        : base(15, "NText", SqlDbType.NText)
    {
    }
}
