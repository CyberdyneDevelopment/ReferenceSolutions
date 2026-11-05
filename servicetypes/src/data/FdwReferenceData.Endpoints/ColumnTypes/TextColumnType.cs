using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server TEXT column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "Text")]
public sealed class TextColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="TextColumnType"/> class.</summary>
    public TextColumnType()
        : base(21, "Text", SqlDbType.Text)
    {
    }
}
