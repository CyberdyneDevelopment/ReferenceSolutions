using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Commands.Data.Abstractions;
using Fdw.Data.Abstractions;
using Fdw.Results;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using System.IO;
using System.Linq;
using Fdw.Data.RowSources.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Http.Abstractions.Results;

using Fdw.Services.Connections.Http;


namespace FdwReferenceConnections.Http;

/// <summary>
/// Generic HTTP connection that works with any protocol (REST, OData, GraphQL, SOAP).
/// Protocol behavior is determined by the injected IHttpProtocol.
/// </summary>
/// <remarks>
/// <para>
/// This connection is protocol-agnostic. The protocol handles:
/// <list type="bullet">
/// <item><description>Translation: IDataCommand → HttpRequestMessage</description></item>
/// <item><description>Response Processing: HttpResponseMessage → typed result</description></item>
/// <item><description>Headers: Protocol-specific header configuration</description></item>
/// <item><description>Security: WS-Security, API keys, etc. (for SOAP protocols)</description></item>
/// </list>
/// </para>
/// <para>
/// Available protocols:
/// <list type="bullet">
/// <item><description>Rest - RESTful APIs with JSON</description></item>
/// <item><description>Soap11 - SOAP 1.1 with XML</description></item>
/// <item><description>Soap12 - SOAP 1.2 with XML</description></item>
/// <item><description>GraphQL - GraphQL endpoints</description></item>
/// <item><description>OData - OData services</description></item>
/// <item><description>Custom protocols extending SoapProtocolBase (e.g., ERCOT)</description></item>
/// </list>
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage] // Excluded: requires HTTP connections
public sealed partial class HttpConnection
    : ConnectionBase<HttpRequestMessage, HttpConnectionConfiguration, HttpConnection>, IHttpConnection,
      IHttpRecordWriterConnection, ISupportsHealthProbe, IRecordSourceConnection
{
    private readonly HttpClient _httpClient;
    private readonly IHttpProtocol _protocol;
    private readonly HttpProtocolContext _context;
    private readonly HttpProtocolTranslatorAdapter _translatorAdapter;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpConnection"/> class.
    /// </summary>
    /// <param name="logger">The logger for this connection.</param>
    /// <param name="configuration">The HTTP connection configuration.</param>
    /// <param name="httpClient">The HTTP client for making requests (pre-configured by factory).</param>
    /// <param name="protocol">The HTTP protocol implementation (Rest, Soap11, Soap12, etc.).</param>
    /// <param name="context">The protocol context with resolved secrets.</param>
    /// <remarks>
    /// The HttpClient should be pre-configured by the factory with BaseAddress, Timeout, and default headers.
    /// The protocol handles translation (IDataCommand → HttpRequestMessage) and response processing.
    /// The context contains resolved secrets (certificates, API keys, passwords) for security.
    /// </remarks>
    public HttpConnection(
        ILogger<HttpConnection> logger,
        HttpConnectionConfiguration configuration,
        HttpClient httpClient,
        IHttpProtocol protocol,
        HttpProtocolContext context)
        : base(logger, configuration)
    {
        _httpClient = httpClient;
        _protocol = protocol;
        _context = context;
        _translatorAdapter = new HttpProtocolTranslatorAdapter(protocol, context);

        LogConnectionInitialized(Logger, configuration.BaseUrl, protocol.Name);
    }

    /// <inheritdoc />
    public string BaseUrl => Configuration.BaseUrl;

    /// <inheritdoc />
    // Why: exposes the pre-configured HttpClient as the typed primitive surface for
    // connectors per §1.1 canary — connectors call this directly rather than going
    // through IDataGateway dispatch.
    public System.Net.Http.HttpClient HttpClient => _httpClient;

    /// <summary>
    /// Tests connectivity to the HTTP endpoint by performing a lightweight request.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the endpoint is reachable.</returns>
    public override async Task<IGenericResult> TestConnection(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, _httpClient.BaseAddress);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            // Why the status is the verdict and not merely logged: a response arriving proves a
            // socket opened and something spoke HTTP, which is not what "connection successful"
            // claims. Without this, a 403, 404 or 503 all report success -- EspnNfl reported a
            // healthy connection while the target was returning 403 to every request. A test that
            // cannot go red teaches people to trust it.
            //
            // Why 401/403 count as failure rather than "reachable but unauthorised": a connection
            // carries its own authentication configuration, so being rejected means the connection
            // AS CONFIGURED does not work, which is the question the operator is asking.
            return response.IsSuccessStatusCode
                ? GenericResult.Success()
                : GenericResult.Failure(
                    HttpResultCodes.ByName("HttpErrorResponse"),
                    ResultDetails.Create(
                        "StatusCode", (int)response.StatusCode,
                        "ReasonPhrase", response.ReasonPhrase ?? string.Empty,
                        "ErrorContent", string.Empty));
        }
        catch (HttpRequestException ex)
        {
            LogRequestException(Logger, ex);
            return GenericResult.Failure(
                HttpResultCodes.ByName("RequestFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            LogRequestTimeout(Logger, ex);
            return GenericResult.Failure(HttpResultCodes.ByName("RequestTimeout"));
        }
    }

    /// <summary>
    /// Performs a cheap liveness probe of the HTTP endpoint by sending a HEAD request to the
    /// configured base address.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating whether the endpoint responded successfully.</returns>
    public async Task<IGenericResult> Probe(CancellationToken cancellationToken = default)
    {
        LogProbeStarting(Logger, _httpClient.BaseAddress);
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Head, _httpClient.BaseAddress);
            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            // The status was already being read to log it and then discarded for the verdict, so a
            // 503 produced "HTTP health probe succeeded with status 503". Same reasoning as
            // TestConnection above -- and this path feeds connection health monitoring, so a probe
            // that always passes makes the whole monitor decorative.
            if (!response.IsSuccessStatusCode)
            {
                LogProbeFailedStatus(Logger, (int)response.StatusCode);
                return GenericResult.Failure(
                    HttpResultCodes.ByName("HttpErrorResponse"),
                    ResultDetails.Create(
                        "StatusCode", (int)response.StatusCode,
                        "ReasonPhrase", response.ReasonPhrase ?? string.Empty,
                        "ErrorContent", string.Empty));
            }

            LogProbeSucceeded(Logger, (int)response.StatusCode);
            return GenericResult.Success();
        }
        catch (HttpRequestException ex)
        {
            LogRequestException(Logger, ex);
            return GenericResult.Failure(
                HttpResultCodes.ByName("RequestFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken != cancellationToken)
        {
            LogRequestTimeout(Logger, ex);
            return GenericResult.Failure(HttpResultCodes.ByName("RequestTimeout"));
        }
        // Why the probe answers every other failure instead of throwing: the health monitor probes
        // connection after connection, and a probe that threw ended the whole pass -- every
        // connection after this one went unprobed. A bad BaseAddress (InvalidOperationException) is
        // one live way to get here. Same shape as the catch-all in the request path above.
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogRequestCancelled(Logger);
            return GenericResult.Failure(HttpResultCodes.ByName("RequestCancelled"));
        }
        catch (Exception ex)
        {
            LogUnexpectedException(Logger, ex);
            return GenericResult.Failure(
                HttpResultCodes.ByName("UnexpectedError"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Why: delegates directly to <see cref="HttpRecordConnector"/> which is internal and carries
    /// the format-factory orchestration. The connection owns the <see cref="HttpClient"/> lifecycle;
    /// the connector is instantiated per-call to remain allocation-light.
    /// </remarks>
    public Task<IGenericResult<int>> WriteRecords(
        IDataContainer container,
        IReadOnlyList<IReadOnlyDictionary<string, object?>> rows,
        CancellationToken cancellationToken = default)
    {
        // Why: connector is created per-call — it is stateless (HttpClient is the state, owned here).
        // Why BaseUrl (not a Name): the typed HttpConnectionConfiguration carries no Name — connection
        // identity lives on the parent header, not the typed body. The connector uses this only for log
        // context, and BaseUrl is the connection's meaningful identity in logs (matches LogConnectionInitialized).
        var connector = new HttpRecordConnector(_httpClient, Configuration.BaseUrl, Logger);
        return connector.Write(container, rows, cancellationToken);
    }

    /// <summary>
    /// Gets the translator for HTTP commands.
    /// Uses an adapter that delegates to the protocol's Translate method.
    /// </summary>
    /// <param name="commandType">The command type (ignored for HTTP - protocol handles all).</param>
    /// <returns>The protocol translator adapter.</returns>
    protected override IDataCommandTranslator<HttpRequestMessage> GetTranslator(string commandType)
        => _translatorAdapter;

    /// <summary>
    /// Executes an HTTP request and returns the typed result.
    /// Works with any HTTP-based protocol (REST, OData, GraphQL, SOAP).
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="command">The HTTP request message to execute.</param>
    /// <param name="container">The storage container for response materialization.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result containing the typed response.</returns>
    protected override async Task<IGenericResult<T>> Execute<T>(
        HttpRequestMessage command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        try
        {
            var method = command.Method.ToString();
            var url = command.RequestUri?.ToString() ?? "";
            LogExecutingRequest(Logger, method, url);

            var response = await _httpClient.SendAsync(command, cancellationToken).ConfigureAwait(false);

            var processResult = await _protocol.ProcessResponse(
                response,
                container,
                typeof(T),
                _context,
                cancellationToken).ConfigureAwait(false);

            if (!processResult.IsSuccess)
            {
                var statusCode = (int)response.StatusCode;
                var reasonPhrase = response.ReasonPhrase ?? "Unknown";
                LogRequestFailed(Logger, statusCode, reasonPhrase, processResult.CurrentMessage ?? "Unknown error");
                return processResult.ToNewResult<T>();
            }

            LogRequestSucceeded(Logger, method, url);
            return ConvertResult<T>(processResult);
        }
        catch (TaskCanceledException ex) when (ex.CancellationToken == cancellationToken)
        {
            LogRequestCancelled(Logger);
            return GenericResult<T>.Failure(HttpResultCodes.ByName("RequestCancelled"));
        }
        catch (TaskCanceledException ex)
        {
            LogRequestTimeout(Logger, ex);
            return GenericResult<T>.Failure(HttpResultCodes.ByName("RequestTimeout"));
        }
        catch (HttpRequestException ex)
        {
            LogRequestException(Logger, ex);
            return GenericResult<T>.Failure(
                HttpResultCodes.ByName("RequestFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
        catch (Exception ex)
        {
            LogUnexpectedException(Logger, ex);
            return GenericResult<T>.Failure(
                HttpResultCodes.ByName("UnexpectedError"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
    }

    private IGenericResult<T> ConvertResult<T>(IGenericResult<object?> processResult)
    {
        if (!processResult.IsSuccess)
            return processResult.ToNewResult<T>();

        if (processResult.Value is null)
            return GenericResult<T>.Success(default!);

        if (processResult.Value is T typedResult)
            return GenericResult<T>.Success(typedResult);

        // Why: the row-extraction path always yields List<Dictionary<string,object?>>, but a DataSet
        // query asks for IEnumerable<IDataRow> — the framework's row. Convert here, where the
        // requested T is known; Convert.ChangeType below cannot produce a row and the plain
        // deserializer cannot construct an interface at all.
        if (processResult.Value is IReadOnlyList<IDictionary<string, object?>> dictRows
            && typeof(T).IsGenericType
            && typeof(T).GetGenericArguments()[0] is { } element
            && (element.Equals(typeof(global::Fdw.Data.DataContainers.Abstractions.IDataRow))
                || element.Equals(typeof(global::Fdw.Data.DataContainers.Abstractions.DataRow))))
        {
            return GenericResult<T>.Success(
                (T)global::Fdw.Data.DataContainers.Abstractions.DataRow.FromDictionaries(dictRows));
        }

        try
        {
            var converted = (T)Convert.ChangeType(processResult.Value, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
            return GenericResult<T>.Success(converted);
        }
        catch (InvalidCastException ex)
        {
            LogDeserializationFailed(Logger, ex, typeof(T).Name, processResult.Value.GetType().Name);
            return GenericResult<T>.Failure(
                HttpResultCodes.ByName("TypeMismatch"),
                ResultDetails.Create()
                    .With("ActualType", processResult.Value.GetType().Name)
                    .With("ExpectedType", typeof(T).Name));
        }
    }

    /// <summary>
    /// Executes an HTTP request without expecting a typed result.
    /// </summary>
    /// <param name="command">The HTTP request message to execute.</param>
    /// <param name="container">The storage container (unused for HTTP connections).</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result indicating success or failure.</returns>
    protected override async Task<IGenericResult> Execute(
        HttpRequestMessage command,
        IStorageContainer container,
        CancellationToken cancellationToken)
    {
        var result = await Execute<object>(command, container, cancellationToken).ConfigureAwait(false);
        return result.IsSuccess
            ? GenericResult.Success()
            : result;
    }

    // Source-generated logging
    [LoggerMessage(EventId = 1, Level = LogLevel.Information,
        Message = "HTTP connection initialized: {BaseUrl} (Protocol: {Protocol})")]
    private static partial void LogConnectionInitialized(ILogger logger, string baseUrl, string protocol);

    [LoggerMessage(EventId = 2, Level = LogLevel.Debug,
        Message = "Executing {Method} request to {Url}")]
    private static partial void LogExecutingRequest(ILogger logger, string method, string url);

    [LoggerMessage(EventId = 3, Level = LogLevel.Information,
        Message = "{Method} request to {Url} succeeded")]
    private static partial void LogRequestSucceeded(ILogger logger, string method, string url);

    [LoggerMessage(EventId = 4, Level = LogLevel.Error,
        Message = "HTTP request failed with status {StatusCode} {ReasonPhrase}: {ErrorContent}")]
    private static partial void LogRequestFailed(ILogger logger, int statusCode, string reasonPhrase, string errorContent);

    [LoggerMessage(EventId = 5, Level = LogLevel.Error,
        Message = "Protocol returned {ActualType} but expected {ExpectedType}")]
    private static partial void LogDeserializationFailed(ILogger logger, Exception exception, string expectedType, string actualType);

    [LoggerMessage(EventId = 6, Level = LogLevel.Warning,
        Message = "Request was cancelled")]
    private static partial void LogRequestCancelled(ILogger logger);

    [LoggerMessage(EventId = 7, Level = LogLevel.Error,
        Message = "Request timed out")]
    private static partial void LogRequestTimeout(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 8, Level = LogLevel.Error,
        Message = "HTTP request exception")]
    private static partial void LogRequestException(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 9, Level = LogLevel.Error,
        Message = "Unexpected exception during HTTP request")]
    private static partial void LogUnexpectedException(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 10, Level = LogLevel.Trace,
        Message = "Probing HTTP connection health: HEAD {BaseUrl}")]
    private static partial void LogProbeStarting(ILogger logger, Uri? baseUrl);

    [LoggerMessage(EventId = 11, Level = LogLevel.Information,
        Message = "HTTP health probe succeeded with status {StatusCode}")]
    private static partial void LogProbeSucceeded(ILogger logger, int statusCode);

    // Why Warning and not Information: the probe reaching a server that answers 4xx or 5xx is a
    // failed health check, and it used to be logged with the success message and that same status
    // number in it.
    [LoggerMessage(EventId = 12, Level = LogLevel.Warning,
        Message = "HTTP health probe failed with status {StatusCode}")]
    private static partial void LogProbeFailedStatus(ILogger logger, int statusCode);

    /// <inheritdoc />
    /// <remarks>
    /// The streaming read of an HTTP source. The response body is handed to the protocol's row
    /// reader for the container's format, so a streamed read and a materialized read of the same
    /// container cannot disagree about what a row is -- they resolve the format and the
    /// RecordSelector/flatten metadata through the same path.
    /// <para>
    /// The returned source owns the response and its stream: the caller iterates lazily, so neither
    /// can be released when this method returns, and disposing the source releases both.
    /// </para>
    /// </remarks>
    public async Task<IGenericResult<IRecordSource<DataRecord>>> OpenRecordSource(
        IDataCommand command, IDataContainer container, CancellationToken cancellationToken = default)
    {
        var translationResult = await GetTranslator(command.CommandType)
            .Translate(command, container, cancellationToken).ConfigureAwait(false);
        if (!translationResult.IsSuccess || translationResult.Value is null)
            return translationResult.ToNewResult<IRecordSource<DataRecord>>();

        var request = translationResult.Value;
        HttpResponseMessage? response = null;
        Stream? stream = null;
        try
        {
            var method = request.Method.ToString();
            var url = request.RequestUri?.ToString() ?? "";
            LogExecutingRequest(Logger, method, url);

            // ResponseHeadersRead: the point of this path is not to buffer the body.
            response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                LogRequestFailed(Logger, status, response.ReasonPhrase ?? "Unknown", "record source");
                return GenericResult<IRecordSource<DataRecord>>.Failure(
                    HttpResultCodes.ByName("RequestFailed"),
                    ResultDetails.Create().With("StatusCode", status));
            }

            stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            var source = _protocol.TryOpenRecordSource(
                container, stream, container.Schema.Fields.OfType<IDataField>().ToList(), Logger);

            if (source is null)
            {
                return GenericResult<IRecordSource<DataRecord>>.Failure(
                    HttpResultCodes.ByName("ResponseRowExtractionFailed"),
                    ResultDetails.Create().With("ErrorMessage",
                        $"No row source reader available for format '{container.Format?.Name}'."));
            }

            var scoped = new ResponseScopedRecordSource(source, response, stream);
            response = null;   // ownership transferred; do not dispose here
            stream = null;
            return GenericResult<IRecordSource<DataRecord>>.Success(scoped);
        }
        catch (HttpRequestException ex)
        {
            return GenericResult<IRecordSource<DataRecord>>.Failure(
                HttpResultCodes.ByName("RequestFailed"),
                ResultDetails.Create().With("ErrorMessage", ex.Message));
        }
        finally
        {
            stream?.Dispose();
            response?.Dispose();
        }
    }

    // The mirror of MsSql's ConnectionScopedRecordSource: a streaming read keeps the response and
    // its content stream open for the cursor's lifetime, so disposal cannot happen at method exit.
    // Disposing this releases the source, the stream and the response. Idempotent.
    private sealed class ResponseScopedRecordSource : IRecordSource<DataRecord>
    {
        private readonly IRecordSource<DataRecord> _inner;
        private readonly HttpResponseMessage _response;
        private readonly Stream _stream;
        private bool _disposed;

        public ResponseScopedRecordSource(
            IRecordSource<DataRecord> inner, HttpResponseMessage response, Stream stream)
        {
            _inner = inner;
            _response = response;
            _stream = stream;
        }

        public RecordSchema Schema => _inner.Schema;

        public IEnumerable<IGenericResult<DataRecord>> Read() => _inner.Read();

        public IAsyncEnumerable<IGenericResult<DataRecord>> Read(CancellationToken cancellationToken = default)
            => _inner.Read(cancellationToken);

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _inner.Dispose();
            _stream.Dispose();
            _response.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            await _inner.DisposeAsync().ConfigureAwait(false);
            await _stream.DisposeAsync().ConfigureAwait(false);
            _response.Dispose();
        }
    }
}
