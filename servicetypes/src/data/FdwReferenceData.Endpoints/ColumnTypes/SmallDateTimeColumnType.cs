using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server SMALLDATETIME column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "SmallDateTime")]
public sealed class SmallDateTimeColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SmallDateTimeColumnType"/> class.</summary>
    public SmallDateTimeColumnType()
        : base(18, "SmallDateTime", SqlDbType.SmallDateTime)
    {
    }
}
