using System;
using System.ComponentModel.DataAnnotations;

namespace ReferenceDataSets.Endpoints;

/// <summary>
/// Request DTO for mapping a DataSet as a source within a parent DataSet composition.
/// </summary>
public class MapDataSetSourceRequest
{
    /// <summary>Gets or sets the parent DataSet name (from route).</summary>
    [Required]
    public string ParentDataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the source name within the parent DataSet (from route).</summary>
    [Required]
    public string SourceName { get; set; } = string.Empty;

    /// <summary>Gets or sets the source DataSet identifier to bind.</summary>
    [Required]
    public Guid SourceDataSetId { get; set; }

    /// <summary>Gets or sets the source DataSet name for display and validation.</summary>
    [Required]
    public string SourceDataSetName { get; set; } = string.Empty;
}
