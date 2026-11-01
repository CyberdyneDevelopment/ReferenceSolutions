using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Response DTO for the DataSet query endpoint when no page limit applies to the caller: every row, written as it is read.
/// </summary>
/// <remarks>
/// The shape of <see cref="QueryDataSetResponse"/> with its rows produced as they are written, and <see cref="RowCount"/> and
/// <see cref="Complete"/> after them, read only then. A response that fails part-way never reaches them: the rows throw, the
/// JSON is left unfinished and the connection is aborted, so a client gets a body that parses and ends in
/// <c>"complete": true</c>, or an error. There is no <c>take</c>: nothing limited the read, and <see cref="HasMoreRows"/> is
/// false because every row from <see cref="Skip"/> on is in <see cref="Rows"/>.
/// </remarks>
public sealed class QueryDataSetStreamResponse
{
    private readonly Func<int> _rowCount;
    private readonly Func<bool> _complete;

    /// <summary>Initializes a new instance of the <see cref="QueryDataSetStreamResponse"/> class.</summary>
    /// <param name="dataSetName">The queried DataSet name.</param>
    /// <param name="columns">The column metadata.</param>
    /// <param name="skip">The number of rows skipped.</param>
    /// <param name="appliedFilters">The filters that were applied.</param>
    /// <param name="rows">The rows as positional value arrays aligned to <paramref name="columns"/>, produced as they are written.</param>
    /// <param name="rowCount">Reads how many rows were produced; asked only after they are written.</param>
    /// <param name="complete">Reads whether every row was produced; asked only after they are written.</param>
    public QueryDataSetStreamResponse(
        string dataSetName,
        IReadOnlyList<QueryDataSetColumnDto> columns,
        int skip,
        IReadOnlyDictionary<string, string> appliedFilters,
        IAsyncEnumerable<object?[]> rows,
        Func<int> rowCount,
        Func<bool> complete)
    {
        DataSetName = dataSetName ?? throw new ArgumentNullException(nameof(dataSetName));
        Columns = columns ?? throw new ArgumentNullException(nameof(columns));
        Skip = skip;

        // Always false, and written as a value of the instance rather than a static or expression-bodied member so it is still
        // serialized: nothing limited the read, so no row follows the ones sent.
        HasMoreRows = false;
        AppliedFilters = appliedFilters ?? throw new ArgumentNullException(nameof(appliedFilters));
        Rows = rows ?? throw new ArgumentNullException(nameof(rows));
        _rowCount = rowCount ?? throw new ArgumentNullException(nameof(rowCount));
        _complete = complete ?? throw new ArgumentNullException(nameof(complete));
    }

    /// <summary>Gets the queried DataSet name.</summary>
    [JsonPropertyOrder(0)]
    public string DataSetName { get; }

    /// <summary>Gets the column metadata. Column ordinal <c>i</c> describes row cell <c>i</c>.</summary>
    [JsonPropertyOrder(1)]
    public IReadOnlyList<QueryDataSetColumnDto> Columns { get; }

    /// <summary>Gets the number of rows skipped.</summary>
    [JsonPropertyOrder(2)]
    public int Skip { get; }

    /// <summary>Gets a value indicating whether more rows exist; false, because every row was asked for.</summary>
    [JsonPropertyOrder(3)]
    public bool HasMoreRows { get; }

    /// <summary>Gets the filters that were applied.</summary>
    [JsonPropertyOrder(4)]
    public IReadOnlyDictionary<string, string> AppliedFilters { get; }

    /// <summary>Gets the result rows as positional value arrays aligned to <see cref="Columns"/>.</summary>
    [JsonPropertyOrder(5)]
    public IAsyncEnumerable<object?[]> Rows { get; }

    /// <summary>Gets how many rows <see cref="Rows"/> carried; read after them, so it is their final count.</summary>
    [JsonPropertyOrder(6)]
    public int RowCount => _rowCount();

    /// <summary>Gets a value indicating whether every row was written; only a response that finished carries it, and it is true.</summary>
    [JsonPropertyOrder(7)]
    public bool Complete => _complete();
}
