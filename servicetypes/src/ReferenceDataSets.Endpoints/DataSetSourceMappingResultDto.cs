using System;

namespace ReferenceDataSets.Endpoints;

/// <summary>
/// Result DTO confirming that a DataSet has been mapped as a source within a parent DataSet composition.
/// </summary>
public class DataSetSourceMappingResultDto
{
    /// <summary>Gets or sets the parent DataSet identifier.</summary>
    public Guid ParentDataSetId { get; set; }

    /// <summary>Gets or sets the parent DataSet name.</summary>
    public string ParentDataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the source name within the parent DataSet.</summary>
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the source DataSet identifier that was bound.</summary>
    public Guid SourceDataSetId { get; set; }

    /// <summary>Gets or sets the source DataSet name.</summary>
    public string SourceDataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the source kind discriminator — always 'DataSet' for this mapping.</summary>
    public string SourceKind { get; set; } = "DataSet";

    /// <summary>Gets or sets the timestamp when the mapping was applied.</summary>
    public DateTimeOffset MappedAt { get; set; }
}
