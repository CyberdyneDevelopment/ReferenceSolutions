using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server VARCHAR column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "VarChar")]
public sealed class VarCharColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="VarCharColumnType"/> class.</summary>
    public VarCharColumnType()
        : base(27, "VarChar", SqlDbType.VarChar)
    {
    }
}
