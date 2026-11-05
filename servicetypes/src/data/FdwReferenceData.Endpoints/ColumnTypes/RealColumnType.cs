using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server REAL column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "Real")]
public sealed class RealColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="RealColumnType"/> class.</summary>
    public RealColumnType()
        : base(17, "Real", SqlDbType.Real)
    {
    }
}
