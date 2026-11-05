using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server NCHAR column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "NChar")]
public sealed class NCharColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="NCharColumnType"/> class.</summary>
    public NCharColumnType()
        : base(14, "NChar", SqlDbType.NChar)
    {
    }
}
