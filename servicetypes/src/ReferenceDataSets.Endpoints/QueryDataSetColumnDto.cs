namespace ReferenceDataSets.Endpoints;

/// <summary>
/// Column metadata in a DataSet query response.
/// </summary>
public sealed class QueryDataSetColumnDto
{
    /// <summary>Gets or sets the field name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the .NET type name.</summary>
    public string DataType { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the field is part of the primary key.</summary>
    public bool IsKey { get; set; }

    /// <summary>Gets or sets whether the field is indexed.</summary>
    public bool IsIndexed { get; set; }

    /// <summary>Gets or sets the field role (Surrogate, NaturalKey, Lookup, Attribute, Measure).</summary>
    public string? Role { get; set; }
}
