namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Request to stream every row of a DataSet.
/// </summary>
public sealed class ExportDataSetRequest
{
    /// <summary>Gets or sets the name of the DataSet to stream.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets whether a delimited or fixed-width file has a header row. A choice of the format asked for, which a stream of that format requires (FDW-3823).</summary>
    public bool? HasHeader { get; set; }

    /// <summary>Gets or sets the delimited separator. A choice of the format asked for, which a stream of that format requires (FDW-3823).</summary>
    public string? Separator { get; set; }

    /// <summary>Gets or sets the one character a delimited field is quoted with. A choice of the format asked for, which a stream of that format requires (FDW-3823).</summary>
    public string? QuoteChar { get; set; }

    /// <summary>Gets or sets whether a delimited file starts with a byte order mark (Excel reads it as UTF-8). A choice of the format asked for, which a stream of that format requires (FDW-3823).</summary>
    public bool? WriteByteOrderMark { get; set; }

    /// <summary>Gets or sets whether a delimited text value that starts like a formula is written with a leading apostrophe. A choice of the format asked for, which a stream of that format requires (FDW-3823).</summary>
    public bool? NeutralizeFormulas { get; set; }

    /// <summary>Gets or sets whether JSON is indented. A choice of the format asked for, which a stream of that format requires (FDW-3823).</summary>
    public bool? WriteIndented { get; set; }

    /// <summary>Gets or sets whether the JSON writer skips its structure validation. A choice of the format asked for, which a stream of that format requires (FDW-3823).</summary>
    public bool? SkipValidation { get; set; }

    /// <summary>Gets or sets the XML root element. A choice of the format asked for, which a stream of that format requires (FDW-3823).</summary>
    public string? RootElementName { get; set; }

    /// <summary>Gets or sets the XML element of each row. A choice of the format asked for, which a stream of that format requires (FDW-3823).</summary>
    public string? RowElementName { get; set; }

    /// <summary>Gets or sets whether XML is indented. A choice of the format asked for, which a stream of that format requires (FDW-3823).</summary>
    public bool? Indent { get; set; }

    /// <summary>Gets or sets whether the XML declaration is left out. A choice of the format asked for, which a stream of that format requires (FDW-3823).</summary>
    public bool? OmitXmlDeclaration { get; set; }

    /// <summary>Gets or sets the name of the first worksheet of an Excel workbook. A choice of the format asked for, which a stream of that format requires (FDW-3823).</summary>
    public string? SheetName { get; set; }
}
