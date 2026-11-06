using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server VARIANT column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "Variant")]
public sealed class VariantColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="VariantColumnType"/> class.</summary>
    public VariantColumnType()
        : base(28, "Variant", SqlDbType.Variant)
    {
    }
}
