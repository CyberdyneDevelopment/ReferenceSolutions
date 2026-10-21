using System;

namespace FdwReferenceDataSets.Endpoints;

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

    /// <summary>Gets or sets the name of the category the data set sits under; null when it sits under none.</summary>
    public string? Category { get; set; }

    /// <summary>Gets or sets the display names from the top of the category tree down to the data set's category, joined by " / "; null when it sits under none.</summary>
    public string? CategoryPath { get; set; }
}
