namespace FdwReferenceConfigurationSchema.Tools;

/// <summary>One column of a <see cref="SchemaKey"/>.</summary>
public sealed class SchemaKeyField
{
    /// <summary>Gets or sets the column name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the column's position within the key.</summary>
    public int Ordinal { get; set; }
}
