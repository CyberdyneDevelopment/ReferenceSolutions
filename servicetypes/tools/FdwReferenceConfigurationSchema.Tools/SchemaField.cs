namespace FdwReferenceConfigurationSchema.Tools;

/// <summary>One column of a <see cref="SchemaContainer"/>, read from sys.columns.</summary>
public sealed class SchemaField
{
    /// <summary>Gets or sets the column name exactly as sys.columns has it, so a renamed column is picked up rather than assumed.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the column's position in the table (sys.columns column_id).</summary>
    public int Ordinal { get; set; }

    /// <summary>Gets or sets the SQL Server type name, without length or precision (for example <c>varchar</c>).</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the column accepts NULL.</summary>
    public bool IsNullable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the database supplies the value — an identity, a
    /// computed column, or one with a default — so a writer may leave it out.
    /// </summary>
    public bool IsSystemProvided { get; set; }

    /// <summary>Gets or sets a value indicating whether the column has a value the database supplies. Set together with <see cref="IsSystemProvided"/>.</summary>
    public bool HasDefault { get; set; }

    /// <summary>Gets or sets a value indicating whether the column is shown to a reader. The generator marks every column visible.</summary>
    public bool Visible { get; set; } = true;
}
