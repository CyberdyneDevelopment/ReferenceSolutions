using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server BINARY column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "Binary")]
public sealed class BinaryColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="BinaryColumnType"/> class.</summary>
    public BinaryColumnType()
        : base(2, "Binary", SqlDbType.Binary)
    {
    }
}
