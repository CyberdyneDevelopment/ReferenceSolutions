using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.Abstractions;
using Fdw.Data.RowSources;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.CommandCapabilities;
using Fdw.Data.RowSources.Delimited.Abstractions;
using Fdw.Data.RowSources.FixedWidth.Abstractions;
using Fdw.Data.RowSources.Json.Abstractions;
using Fdw.Data.RowSources.Xml.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections.Http.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Fdw.Services.Connections.Http;

namespace ReferenceConnections.Http;

/// <summary>
/// Runs the config-driven record write for an HTTP connection: serializes rows through
/// <c>RecordWriterTypes.ByName(container.Format.Name).Create(context)</c> and POSTs/PUTs the
/// serialized body to the endpoint declared in the container's metadata (from
/// <see cref="HttpRecordWriterCapability"/>). No per-format branching in the transport layer —
/// adding a new format adds a <c>RecordWriterType</c>, not a branch here.
/// </summary>
/// <remarks>
/// Why a dedicated connector class rather than inlining in the connection: mirrors
/// <c>FileSystemRecordConnector</c> — isolates format-factory orchestration from the connection's
/// translator/Execute plumbing, and is unit-testable with an injected <see cref="HttpClient"/>.
/// </remarks>
public sealed class HttpRecordConnector
{
    private readonly HttpClient _httpClient;
    private readonly string _connectionName;
    private readonly ILogger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpRecordConnector"/> class.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> scoped to the connection's base URL.</param>
    /// <param name="connectionName">The owning connection name, for structured logging.</param>
    /// <param name="logger">Logger; falls back to <see cref="NullLogger"/> when null.</param>
    public HttpRecordConnector(HttpClient httpClient, string connectionName, ILogger? logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _connectionName = connectionName;
        // Why: NullLogger keeps the connector functional without DI logging — the only sanctioned ?? fallback.
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Writes a batch of records to the HTTP endpoint configured on the container, serializing through
    /// the container's configured format and field schema.
    /// </summary>
    /// <param name="container">
    /// The configured container (format + field schema + endpoint metadata from the
    /// <see cref="HttpRecordWriterCapability"/> fields).
    /// </param>
    /// <param name="rows">The rows to write as flat name→value maps.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A success result carrying the written record count, or a failure carrying the error.</returns>
    public async Task<IGenericResult<int>> Write(
        IDataContainer container,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        var formatResult = ResolveWriterType(container);
        if (!formatResult.IsSuccess)
        {
            return formatResult.ToNewResult<int>();
        }

        var endpointResult = ResolveEndpoint(container);
        if (!endpointResult.IsSuccess)
        {
            return endpointResult.ToNewResult<int>();
        }

        var (endpoint, method, contentType) = endpointResult.Value!;

        HttpRecordConnectorLog.WritingRecords(_logger, _connectionName, rows.Count, endpoint);

        var fields = Fields(container);
        var buffer = new StringBuilder();
        using (var target = new StringWriter(buffer))
        {
            var context = new RecordWriterContext(target, fields, BuildWriterOptions(container));
            using var writer = formatResult.Value!.Create(context);

            // Why: row writers (delimited/fixed-width) take the flat name→value dictionary directly via
            // IRowWriter; item writers (Json/Xml) take a DataRecord projected over the container schema.
            WriteRows(writer, new RecordSchema(fields), rows, cancellationToken);
            writer.Flush();
        }

        using var content = new StringContent(buffer.ToString(), Encoding.UTF8, contentType);
        using var request = new HttpRequestMessage(method, endpoint) { Content = content };
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            return GenericResult<int>.Failure(
                HttpRecordConnectorLog.HttpSendFailed(
                    _logger, _connectionName, endpoint, (int)response.StatusCode));
        }

        HttpRecordConnectorLog.WriteRecordsCompleted(_logger, _connectionName, rows.Count, endpoint);
        return GenericResult<int>.Success(rows.Count);
    }

    // Why: row writers (delimited/fixed-width) implement IRowWriter and take the flat name→value
    // dictionary directly. Item writers (Json/Xml) implement only IRecordWriter<DataRecord>, so each row
    // is projected into a DataRecord over the shared container schema.
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

    private IGenericResult<IRecordWriterType> ResolveWriterType(IDataContainer container)
    {
        if (container.Format is null || string.IsNullOrEmpty(container.Format.Name))
        {
            return GenericResult<IRecordWriterType>.Failure(
                HttpRecordConnectorLog.FormatNotConfigured(_logger, _connectionName, container.Name));
        }

        var writerType = RecordWriterTypes.ByName(container.Format.Name);
        if (writerType == RecordWriterTypes.NotFound)
        {
            return GenericResult<IRecordWriterType>.Failure(
                HttpRecordConnectorLog.FormatNotRegistered(_logger, _connectionName, container.Format.Name));
        }

        return GenericResult<IRecordWriterType>.Success(writerType);
    }

    // Why: endpoint + method + content-type are stored in the container's Metadata under the keys defined
    // by HttpRecordWriterCapability. Endpoint and Method are required; ContentType is optional but must
    // be non-empty when present — an absent ContentType is also a failure so the caller explicitly
    // configures a media type rather than getting a silent default (NO FALLBACKS).
    private IGenericResult<(string endpoint, HttpMethod method, string contentType)> ResolveEndpoint(
        IDataContainer container)
    {
        if (!container.Metadata.TryGetValue("Endpoint", out var endpointObj)
            || endpointObj is not string endpoint
            || string.IsNullOrWhiteSpace(endpoint))
        {
            return GenericResult<(string, HttpMethod, string)>.Failure(
                HttpRecordConnectorLog.FormatNotConfigured(_logger, _connectionName, container.Name));
        }

        if (!container.Metadata.TryGetValue("Method", out var methodObj)
            || methodObj is not string methodName
            || string.IsNullOrWhiteSpace(methodName))
        {
            return GenericResult<(string, HttpMethod, string)>.Failure(
                HttpRecordConnectorLog.FormatNotConfigured(_logger, _connectionName, container.Name));
        }

        if (!container.Metadata.TryGetValue("ContentType", out var ctObj)
            || ctObj is not string contentType
            || string.IsNullOrWhiteSpace(contentType))
        {
            return GenericResult<(string, HttpMethod, string)>.Failure(
                HttpRecordConnectorLog.FormatNotConfigured(_logger, _connectionName, container.Name));
        }

        // Why: HttpMethod is a value object — new HttpMethod("PUT") equals HttpMethod.Put by value. Using
        // the constructor rather than a switch keeps the connector open to non-standard methods without a
        // special-case branch.
        return GenericResult<(string, HttpMethod, string)>.Success(
            (endpoint, new HttpMethod(methodName), contentType));
    }

    // Why: the write-side options mirror ContainerRecordOptions.BuildWriterOptions in the FileSystem
    // connector. The Http project already carries all four format Abstractions references (for the read
    // path), so no new project reference is required.
    private static RowWriterOptions? BuildWriterOptions(IStorageContainer container)
        => container.Format.Name switch
        {
            "Json" => BuildJsonWriterOptions(container.Metadata),
            "Xml" => BuildXmlWriterOptions(container.Metadata),
            "Delimited" => BuildDelimitedWriterOptions(container),
            "FixedWidth" => BuildFixedWidthWriterOptions(container),
            _ => null
        };

    private static JsonRowWriterOptions BuildJsonWriterOptions(IReadOnlyDictionary<string, object> meta)
        => new()
        {
            WriteIndented = meta.TryGetValue("WriteIndented", out var wi) && wi is bool wb && wb
        };

    private static XmlRowWriterOptions BuildXmlWriterOptions(IReadOnlyDictionary<string, object> meta)
    {
        var options = new XmlRowWriterOptions();
        if (meta.TryGetValue("RowElementName", out var rn) && rn is string rns && !string.IsNullOrEmpty(rns))
            options.RowElementName = rns;
        if (meta.TryGetValue("RootElementName", out var root) && root is string roots && !string.IsNullOrEmpty(roots))
            options.RootElementName = roots;
        return options;
    }

    private static DelimitedRowWriterOptions BuildDelimitedWriterOptions(IStorageContainer container)
    {
        var meta = container.Metadata;
        var options = new DelimitedRowWriterOptions
        {
            WriteHeader = meta.TryGetValue("HasHeader", out var hh) && hh is bool hb && hb,
            Separator = meta.TryGetValue("Separator", out var sep) && sep is string ss && !string.IsNullOrEmpty(ss)
                ? ss
                : ","
        };
        options.Columns = new List<string>(FieldNames(container));
        return options;
    }

    private static FixedWidthRowWriterOptions BuildFixedWidthWriterOptions(IStorageContainer container)
    {
        var options = new FixedWidthRowWriterOptions
        {
            WriteHeader = container.Metadata.TryGetValue("HasHeader", out var hh) && hh is bool hb && hb
        };
        options.Fields = new List<FixedWidthField>(FixedWidthFields(container));
        return options;
    }

    private static IEnumerable<string> FieldNames(IStorageContainer container)
    {
        var fields = container.Schema?.Fields;
        if (fields is null) yield break;
        foreach (var field in fields) yield return field.Name;
    }

    private static IEnumerable<FixedWidthField> FixedWidthFields(IStorageContainer container)
    {
        var fields = container.Schema?.Fields;
        if (fields is null) yield break;
        foreach (var field in fields)
        {
            var fieldMeta = field.Metadata;
            if (fieldMeta is null
                || !fieldMeta.TryGetValue("StartIndex", out var startObj)
                || !fieldMeta.TryGetValue("Length", out var lenObj))
            {
                continue;
            }

            yield return new FixedWidthField
            {
                Name = field.Name,
                StartIndex = Convert.ToInt32(startObj, CultureInfo.InvariantCulture),
                Length = Convert.ToInt32(lenObj, CultureInfo.InvariantCulture)
            };
        }
    }

    // Why: the flyweight schema for the record writer is the container's IDataField children.
    // IDataContainer.Nodes is the field child set; project to IDataField — a child that is not an
    // IDataField is a contract violation, surfaced immediately.
    private static List<IDataField> Fields(IDataContainer container)
        => container.Nodes
            .Select(n => n as IDataField
                ?? throw new InvalidOperationException(
                    $"Container '{container.Name}' child node '{n.Name}' is not an IDataField."))
            .ToList();
}
