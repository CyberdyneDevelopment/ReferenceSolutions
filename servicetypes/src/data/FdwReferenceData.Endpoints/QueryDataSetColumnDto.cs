using System.Collections.Generic;

namespace FdwReferenceDataSets.Endpoints;

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

    /// <summary>Gets or sets whether API callers may filter on the field.</summary>
    public bool IsFilterable { get; set; }

    /// <summary>
    /// Gets or sets the operators a filter on this field may use, by name; empty when the field is not filterable or is not
    /// yet typed.
    /// </summary>
    public List<string> FilterOperators { get; set; } = [];

    /// <summary>Gets or sets the field role (Surrogate, NaturalKey, Lookup, Attribute, Measure).</summary>
    public string? Role { get; set; }
}
