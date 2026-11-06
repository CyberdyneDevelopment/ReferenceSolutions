using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server VARBINARY column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "VarBinary")]
public sealed class VarBinaryColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="VarBinaryColumnType"/> class.</summary>
    public VarBinaryColumnType()
        : base(26, "VarBinary", SqlDbType.VarBinary)
    {
    }
}
