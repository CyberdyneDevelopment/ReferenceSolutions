using System.ComponentModel.DataAnnotations;

namespace Reference.Api.Endpoints;

/// <summary>
/// Request DTO for searching available DataSets to bind as a source in compound/federated composition.
/// </summary>
public class SearchDataSetSourceRequest
{
    /// <summary>Gets or sets the parent DataSet name (from route).</summary>
    [Required]
    public string ParentDataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional search term to filter candidate DataSets by name or display name.</summary>
    public string? SearchTerm { get; set; }
}
