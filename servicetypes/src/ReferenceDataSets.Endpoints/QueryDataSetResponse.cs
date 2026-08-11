using System.Collections.Generic;

namespace ReferenceDataSets.Endpoints;

/// <summary>
/// Response DTO for the DataSet query endpoint.
/// </summary>
public sealed class QueryDataSetResponse
{
    /// <summary>Gets or sets the queried DataSet name.</summary>
    public string DataSetName { get; set; } = string.Empty;

    /// <summary>Gets or sets the column metadata. Column ordinal <c>i</c> describes row cell <c>i</c>.</summary>
    public IReadOnlyList<QueryDataSetColumnDto> Columns { get; set; } = [];

    /// <summary>
    /// Gets or sets the result rows as positional value arrays aligned to <see cref="Columns"/>.
    /// </summary>
    /// <remarks>
    /// Why positional <c>object?[]</c> instead of a per-row name→value map: the rows stream off the
    /// record-source cursor where the column schema is described once (in <see cref="Columns"/>) and
    /// shared across every row. Emitting a dictionary per row would re-key the column names on every
    /// row — exactly the allocation the streaming cursor exists to avoid. Cell <c>j</c> of every row
    /// corresponds to <c>Columns[j]</c>.
    /// </remarks>
    public IReadOnlyList<object?[]> Rows { get; set; } = [];

    /// <summary>Gets or sets the number of rows skipped.</summary>
    public int Skip { get; set; }

    /// <summary>Gets or sets the requested page size.</summary>
    public int Take { get; set; }

    /// <summary>Gets or sets whether more rows exist beyond this page.</summary>
    public bool HasMoreRows { get; set; }

    /// <summary>Gets or sets the filters that were applied.</summary>
    public IReadOnlyDictionary<string, string> AppliedFilters { get; set; } = new Dictionary<string, string>();
}
