using FastEndpoints;

namespace FdwReferenceDataSets.Endpoints;

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

    /// <summary>
    /// Gets or sets the maximum number of rows to return. When omitted, the <c>DefaultPaginationSize</c> setting applies and a
    /// value above <c>MaxPaginationSize</c> is capped to it; a caller authenticated with an API key is sized by the ApiKey
    /// settings instead, and with no limit set is sent every row (FDW-1209).
    /// </summary>
    [QueryParam]
    public int? Take { get; set; }
}
