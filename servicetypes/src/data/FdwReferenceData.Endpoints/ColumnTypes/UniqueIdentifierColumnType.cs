using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server UNIQUEIDENTIFIER column type (also written GUID).</summary>
[TypeOption(typeof(SqlColumnTypes), "UniqueIdentifier")]
public sealed class UniqueIdentifierColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="UniqueIdentifierColumnType"/> class.</summary>
    public UniqueIdentifierColumnType()
        : base(25, "UniqueIdentifier", SqlDbType.UniqueIdentifier, "GUID")
    {
    }
}
