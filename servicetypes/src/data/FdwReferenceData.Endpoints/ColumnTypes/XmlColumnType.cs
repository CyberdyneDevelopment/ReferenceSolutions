using System.Data;
using Fdw.Collections.Attributes;

namespace FdwReferenceDdl.Endpoints.ColumnTypes;

/// <summary>The SQL Server XML column type.</summary>
[TypeOption(typeof(SqlColumnTypes), "Xml")]
public sealed class XmlColumnType : SqlColumnTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="XmlColumnType"/> class.</summary>
    public XmlColumnType()
        : base(29, "Xml", SqlDbType.Xml)
    {
    }
}
