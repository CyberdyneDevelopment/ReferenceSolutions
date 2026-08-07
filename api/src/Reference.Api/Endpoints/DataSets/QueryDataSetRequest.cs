using FastEndpoints;

namespace Reference.Api.Endpoints;

/// <summary>
/// Request DTO for the DataSet query endpoint.
/// Dynamic field filters are extracted from the query string at runtime.
/// </summary>
public sealed class QueryDataSetRequest
{
    /// <summary>Gets or sets the DataSet name (path parameter).</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of rows to skip.</summary>
    [QueryParam]
    public int Skip { get; set; }

    /// <summary>Gets or sets the maximum number of rows to return (default 50, max 1000).</summary>
    [QueryParam]
    public int Take { get; set; } = 50;
}
