using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server IMAGE column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "Image")]
public sealed class ImageColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="ImageColumnType"/> class.</summary>
    public ImageColumnType()
        : base(11, "Image", SqlDbType.Image)
    {
    }
}
