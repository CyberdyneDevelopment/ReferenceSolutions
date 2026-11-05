using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server SMALLINT column type (also written SHORT).</summary>
[TypeOption(typeof(SqlColumnTypes), "SmallInt")]
public sealed class SmallIntColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="SmallIntColumnType"/> class.</summary>
    public SmallIntColumnType()
        : base(19, "SmallInt", SqlDbType.SmallInt, "SHORT")
    {
    }
}
