using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server NVARCHAR column type (also written STRING).</summary>
[TypeOption(typeof(SqlColumnTypes), "NVarChar")]
public sealed class NVarCharColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="NVarCharColumnType"/> class.</summary>
    public NVarCharColumnType()
        : base(16, "NVarChar", SqlDbType.NVarChar, "STRING")
    {
    }
}
