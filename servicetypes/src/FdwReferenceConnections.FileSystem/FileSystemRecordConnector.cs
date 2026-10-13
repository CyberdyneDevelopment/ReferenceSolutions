using FdwReferenceData.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Results;
using FdwReferenceConnections.FileSystem.Abstractions;
using FdwReferenceConnections.FileSystem.Abstractions.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.FileSystem;

/// <summary>
/// Runs the config-driven record read/write for the FileSystem connection: reading a configured file
/// container opens the file stream and feeds it to
/// <c>RecordSourceTypes.ByName(container.Format.Name).Create(stream, container config)</c>; writing
/// serializes rows through <c>RecordWriterTypes.ByName(container.Format.Name).Create(target, container config)</c>
/// and persists the result to the file. The container's configured fields ARE the schema — no per-format
/// container class and no compile-time DTO.
/// </summary>
/// <remarks>
/// Why a dedicated connector class rather than inlining in the connection: it isolates the format-factory
/// orchestration (resolve type by format → build options from the container → Create(context) → enumerate
/// / write) from the connection's translator/Execute plumbing, and it is unit-testable with an
/// <see cref="IFileSystemClient"/> over a temp directory. The connector NEVER names a concrete reader or
/// writer type — adding a format adds a <c>RecordSourceType</c>/<c>RecordWriterType</c>, not a branch here.
/// </remarks>
internal sealed class FileSystemRecordConnector
{
    private readonly IFileSystemClient _client;
    private readonly string _connectionName;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSystemRecordConnector"/> class.
    /// </summary>
    /// <param name="client">The path-isolated file client that performs the actual I/O.</param>
    /// <param name="connectionName">The owning connection name, for structured logging.</param>
    /// <param name="logger">Logger; falls back to <see cref="NullLogger"/> when null.</param>
    public FileSystemRecordConnector(IFileSystemClient client, string connectionName, ILogger? logger)
    {
        _client = client;
        _connectionName = connectionName;
        // Why: NullLogger keeps the connector functional without DI logging — the only sanctioned ?? fallback.
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Reads every record from the configured file container as a flat name→value row, projected through
    /// the container's field schema.
    /// </summary>
    /// <param name="container">The configured container (format + field schema + physical file path).</param>
    /// <param name="relativePath">The file path relative to the connection root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A success result carrying the read rows, or a failure carrying the structured read/format error.
    /// </returns>
    public async Task<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> Read(
        IDataContainer container,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var formatResult = ResolveSourceType(container);
        if (!formatResult.IsSuccess)
        {
            return formatResult.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
        }

        FileSystemRecordConnectorLog.ReadingRecords(
            _logger, _connectionName, container.Name, container.Format.Name, relativePath);

        var contentResult = await ReadContent(relativePath, formatResult.Value!.ReadsBinary, cancellationToken).ConfigureAwait(false);
        if (!contentResult.IsSuccess)
        {
            return contentResult.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
        }

        var fieldsResult = Fields(container);
        if (!fieldsResult.IsSuccess)
        {
            return fieldsResult.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
        }

        // Why: the format factory reads from a Stream; a text format's content is the file decoded as text and
        // re-encoded UTF-8, a binary format's (an Excel workbook is a zip) is the file's own bytes -- see ReadContent --
        // so the same Create(context) seam the HTTP path uses drives the parse.
        using var stream = new MemoryStream(contentResult.Value!);
        var context = new RecordSourceContext(stream, fieldsResult.Value!, ContainerRecordOptions.BuildSourceOptions(container));
        using var source = formatResult.Value!.Create(context);

        // Why: sources that expose IRowSource.Cursor (Delimited/FixedWidth AND Json — see JsonRowSourceType)
        // read the FULL row from the cursor's own field set, not the schema-projected DataRecord — for
        // Delimited/FixedWidth this is behaviorally identical (their Columns/Fields options are BUILT FROM
        // the declared schema, so the cursor's field set always equals it), but for Json it preserves any
        // column beyond the declared schema (the cursor tracks whatever properties the source JSON object
        // actually carries). Xml has no cursor (genuinely non-tabular document shape) and falls back to the
        // schema-projected DataRecord — an undeclared Xml column is out of scope for this fix (see
        // FileSystemRecordConnector.WriteRows's identical Xml note on the write side).
        var rowSource = source as IRowSource;
        var rows = new List<IReadOnlyDictionary<string, object?>>();
        foreach (var recordResult in source.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!recordResult.IsSuccess)
            {
                // Why: a per-record parse failure surfaces as a failed enumeration element; propagate it
                // rather than silently dropping the row (NO FALLBACKS).
                return recordResult.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();
            }

            rows.Add(rowSource is not null ? BuildFullRow(rowSource.Cursor) : recordResult.Value.ToDictionary());
        }

        FileSystemRecordConnectorLog.ReadRecordsCompleted(_logger, _connectionName, container.Name, rows.Count);
        return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(rows);
    }

    // Why a binary format is read as bytes (FDW-1308): decoding a workbook as text corrupts it. The text path is as it
    // was: the file decoded, then encoded UTF-8 for the reader.
    private async Task<IGenericResult<byte[]>> ReadContent(string relativePath, bool binary, CancellationToken cancellationToken)
    {
        if (binary)
        {
            return await _client.ReadBytes(relativePath, cancellationToken).ConfigureAwait(false);
        }

        var text = await _client.ReadText(relativePath, cancellationToken).ConfigureAwait(false);
        return text.IsSuccess
            ? GenericResult<byte[]>.Success(Encoding.UTF8.GetBytes(text.Value!))
            : text.ToNewResult<byte[]>();
    }

    // Why: reads EVERY field the cursor currently exposes (not just the container's declared schema) into
    // a flat name→value map — the full-fidelity counterpart to DataRecord.ToDictionary(), which is capped
    // to the declared schema. Mirrors RecordDictionaryReader's own "column superset, null for rows missing
    // a column" convention: a field the CURRENT row doesn't carry (because an earlier/later row in the same
    // file introduced it) reads null here, same as everywhere else in this codebase absence is tolerated.
    private static Dictionary<string, object?> BuildFullRow(IRecordCursor cursor)
    {
        var row = new Dictionary<string, object?>(cursor.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < cursor.FieldCount; i++)
        {
            row[cursor.GetFieldName(i)] = cursor.IsNull(i) ? null : cursor.GetValue(i);
        }

        return row;
    }

    /// <summary>
    /// Reads every record from the configured file container as with <see cref="Read"/>, but returns an
    /// EMPTY row set when the backing file does not yet exist (rather than a file-not-found failure).
    /// </summary>
    /// <param name="container">The configured container (format + field schema + physical file path).</param>
    /// <param name="relativePath">The file path relative to the connection root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// The decoded rows, an empty list when the file is absent, or a failure carrying the read/format error.
    /// </returns>
    /// <remarks>
    /// Why: the version-on-write write verbs (save/update/delete) read the container's current rows,
    /// mutate them in memory, and rewrite the whole file. A first-ever save targets a file that does not
    /// exist yet — that is "no current rows", not an error. An update/delete against an absent file
    /// legitimately affects zero rows. Distinguishing genuine absence (Exists=false) from an I/O failure
    /// keeps the write path fail-loud on real errors while treating a missing file as an empty set.
    /// </remarks>
    public async Task<IGenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>> ReadExistingOrEmpty(
        IDataContainer container,
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var existsResult = await _client.Exists(relativePath, cancellationToken).ConfigureAwait(false);
        if (!existsResult.IsSuccess)
            return existsResult.ToNewResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>();

        if (!existsResult.Value)
            return GenericResult<IReadOnlyList<IReadOnlyDictionary<string, object?>>>.Success(
                new List<IReadOnlyDictionary<string, object?>>());

        return await Read(container, relativePath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Writes the supplied rows to the configured file container, serializing through the container's
    /// configured format and field schema.
    /// </summary>
    /// <param name="container">The configured container (format + field schema + physical file path).</param>
    /// <param name="relativePath">The file path relative to the connection root.</param>
    /// <param name="rows">The rows to write as flat name→value maps.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success result carrying the written record count, or a failure carrying the error.</returns>
    public async Task<IGenericResult<int>> Write(
        IDataContainer container,
        string relativePath,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        var formatResult = ResolveWriterType(container);
        if (!formatResult.IsSuccess)
        {
            return formatResult.ToNewResult<int>();
        }

        FileSystemRecordConnectorLog.WritingRecords(
            _logger, _connectionName, container.Name, container.Format.Name, relativePath);

        var fieldsResult = Fields(container);
        if (!fieldsResult.IsSuccess)
        {
            return fieldsResult.ToNewResult<int>();
        }

        var fields = fieldsResult.Value!;
        if (formatResult.Value!.WritesBinary)
        {
            return await WriteBinary(container, relativePath, formatResult.Value, fields, rows, cancellationToken).ConfigureAwait(false);
        }

        var buffer = new StringBuilder();
        using (var target = new StringWriter(buffer))
        {
            var context = new RecordWriterContext(target, fields, ContainerRecordOptions.BuildWriterOptions(container));
            using var writer = formatResult.Value!.Create(context);

            // Why: row writers (delimited/fixed-width) take the flat name→value dictionary directly via
            // IRowWriter; item writers (Json/Xml) take a DataRecord projected over the container schema.
            // Both reach the writer through the record-writer surface the factory returned — no concrete
            // writer type is named here.
            WriteRows(writer, new RecordSchema(fields), rows, cancellationToken);
            writer.Flush();
        }

        var writeResult = await _client.WriteText(relativePath, buffer.ToString(), cancellationToken).ConfigureAwait(false);
        if (!writeResult.IsSuccess)
        {
            return writeResult.ToNewResult<int>();
        }

        FileSystemRecordConnectorLog.WriteRecordsCompleted(_logger, _connectionName, container.Name, rows.Count);
        return GenericResult<int>.Success(rows.Count);
    }

    // Why a binary format has its own write (FDW-1298): a workbook is a zip, so it has no string form. It is built through the same
    // format-driven factory as every other format (the writer type's Create over a byte context), then handed to the client as bytes.
    private async Task<IGenericResult<int>> WriteBinary(
        IDataContainer container,
        string relativePath,
        IRecordWriterType format,
        List<IDataField> fields,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken)
    {
        var bytes = RecordWriterBytes.Write(
            format,
            fields,
            ContainerRecordOptions.BuildWriterOptions(container),
            writer => WriteRows(writer, new RecordSchema(fields), rows, cancellationToken));
        var written = await _client.WriteBytes(relativePath, bytes, cancellationToken).ConfigureAwait(false);
        if (!written.IsSuccess)
        {
            return written.ToNewResult<int>();
        }

        FileSystemRecordConnectorLog.WriteRecordsCompleted(_logger, _connectionName, container.Name, rows.Count);
        return GenericResult<int>.Success(rows.Count);
    }

    // Why: writers that can accept a FULL, schema-agnostic row without losing columns — row writers
    // (delimited/fixed-width, genuinely tabular) AND Json (no fixed-column constraint at all) — implement
    // IRowWriter and take the flat name→value dictionary directly, preserving every key the row carries,
    // including ones the container's declared field schema doesn't. Xml implements only
    // IRecordWriter<DataRecord>, so each row is projected into a DataRecord over the shared container
    // schema (DataRecord.ToDictionary(), called inside the item writer, emits one object per row keyed by
    // the schema field names) — any row key beyond the declared schema is dropped for Xml specifically.
    private static void WriteRows(
        IRecordWriter<DataRecord> writer,
        RecordSchema schema,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken)
    {
        if (writer is IRowWriter rowWriter)
        {
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                rowWriter.Write(row);
            }

            return;
        }

        foreach (var row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            writer.Write(ToRecord(schema, row));
        }
    }

    // Why: align the dictionary's values to the schema's field order so the DataRecord's value array
    // matches the flyweight schema (DataRecord's ctor fails loud on a length mismatch). A field absent
    // from the row reads null.
    private static DataRecord ToRecord(RecordSchema schema, IReadOnlyDictionary<string, object?> row)
    {
        var values = new object?[schema.FieldCount];
        for (var i = 0; i < schema.FieldCount; i++)
        {
            values[i] = row.TryGetValue(schema.GetFieldName(i), out var value) ? value : null;
        }

        return new DataRecord(schema, values);
    }

    private IGenericResult<IRecordSourceType> ResolveSourceType(IDataContainer container)
    {
        if (container.Format is null || string.IsNullOrEmpty(container.Format.Name))
        {
            return GenericResult<IRecordSourceType>.Failure(
                FileSystemRecordConnectorLog.FormatNotConfigured(_logger, _connectionName, container.Name));
        }

        var sourceType = RecordSourceTypes.ByName(container.Format.Family);
        if (sourceType == RecordSourceTypes.NotFound)
        {
            return GenericResult<IRecordSourceType>.Failure(
                FileSystemRecordConnectorLog.FormatNotRegistered(_logger, _connectionName, container.Format.Name));
        }

        return GenericResult<IRecordSourceType>.Success(sourceType);
    }

    private IGenericResult<IRecordWriterType> ResolveWriterType(IDataContainer container)
    {
        if (container.Format is null || string.IsNullOrEmpty(container.Format.Name))
        {
            return GenericResult<IRecordWriterType>.Failure(
                FileSystemRecordConnectorLog.FormatNotConfigured(_logger, _connectionName, container.Name));
        }

        var writerType = RecordWriterTypes.ByName(container.Format.Family);
        if (writerType == RecordWriterTypes.NotFound)
        {
            return GenericResult<IRecordWriterType>.Failure(
                FileSystemRecordConnectorLog.FormatNotRegistered(_logger, _connectionName, container.Format.Name));
        }

        return GenericResult<IRecordWriterType>.Success(writerType);
    }

    // Why: the flyweight schema for the record source/writer is the container's IDataField children.
    // IDataContainer.Nodes is the field child set (covariant IReadOnlyList<IDataField>); project to the
    // concrete field type the contexts require. A child that is not an IDataField is a configuration
    // defect the caller decides how to handle, so it comes back as a Failure like every other step in
    // Read()/Write(), not a thrown exception.
    private IGenericResult<List<IDataField>> Fields(IDataContainer container)
    {
        var fields = new List<IDataField>();
        foreach (var node in container.Nodes)
        {
            if (node is not IDataField field)
            {
                return GenericResult<List<IDataField>>.Failure(
                    FileSystemRecordConnectorLog.FieldNotIDataField(_logger, _connectionName, container.Name, node.Name));
            }

            fields.Add(field);
        }

        return GenericResult<List<IDataField>>.Success(fields);
    }
}
