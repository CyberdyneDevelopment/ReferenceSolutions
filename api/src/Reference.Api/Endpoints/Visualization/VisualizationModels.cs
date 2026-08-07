using System.Collections.Generic;
using Fdw.Services.Data.Abstractions.Visualization;

namespace Reference.Api.Endpoints.Visualization;

/// <summary>Response model for listing available visualization types.</summary>
public sealed class VisualizationTypeListResponse
{
    /// <summary>Gets or sets the available visualization types.</summary>
    public IReadOnlyList<VisualizationTypeItem> Types { get; set; } = [];
}

/// <summary>Lightweight representation of a visualization type for API responses.</summary>
public sealed class VisualizationTypeItem
{
    /// <summary>Gets or sets the visualization type name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Gets or sets the icon identifier.</summary>
    public string Icon { get; set; } = string.Empty;
}

/// <summary>Request model for the calculation pipeline endpoint.</summary>
public sealed class CalculatePipelineRequest
{
    /// <summary>Gets or sets the calculations to apply.</summary>
    public IReadOnlyList<ColumnCalculation> Calculations { get; set; } = [];

    /// <summary>Gets or sets the source data rows.</summary>
    public IReadOnlyList<Dictionary<string, object?>> Rows { get; set; } = [];
}

/// <summary>Response model for the calculation pipeline endpoint.</summary>
public sealed class CalculatePipelineResponse
{
    /// <summary>Gets or sets the result rows with computed columns.</summary>
    public IReadOnlyList<IReadOnlyDictionary<string, object?>> Rows { get; set; } = [];
}
