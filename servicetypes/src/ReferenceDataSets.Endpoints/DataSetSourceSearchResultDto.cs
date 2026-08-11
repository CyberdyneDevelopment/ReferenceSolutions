using System;

namespace ReferenceDataSets.Endpoints;

/// <summary>
/// Result DTO representing a DataSet available as a candidate source for compound/federated composition.
/// </summary>
public class DataSetSourceSearchResultDto
{
    /// <summary>Gets or sets the unique DataSet identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the DataSet name (stable logical identifier).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the DataSet category.</summary>
    public string Category { get; set; } = string.Empty;
}
