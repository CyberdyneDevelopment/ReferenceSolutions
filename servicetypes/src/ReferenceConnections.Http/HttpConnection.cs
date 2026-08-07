using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Commands.Data.Abstractions;
using Fdw.Configuration;
using Fdw.Data.Abstractions;
using Fdw.Data.Http.Containers;
using Fdw.Data.Http.Paths;
using Fdw.Results;
using Fdw.Results.Abstractions;
using Fdw.Services.Connections;
using Fdw.Services.Connections.Abstractions;
using Fdw.Services.Connections.Http.Abstractions;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes;
using Fdw.Services.Connections.Http.Abstractions.OptionTypes.HttpProtocolOptions;
using Fdw.Services.Connections.Http.Abstractions.Results;

using Fdw.Services.Connections.Http;

using Fdw.Services.Connections.Http.Limits;

using Fdw.Services.Connections.Http.Results;

using Fdw.Services.Connections.Http.Logging;

using Fdw.Services.Connections.Http.Commands;

using Fdw.Services.Connections.Http.Validation;

using Fdw.Services.Connections.Http.Protocols;

using Fdw.Services.Connections.Http.Security;


namespace ReferenceConnections.Http;

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
      IHttpRecordWriterConnection, ISupportsHealthProbe
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

    /// <summary>
    /// Gets a value indicating whether this HTTP connection is stale.
    /// HTTP connections are stateless and never stale.
    /// </summary>
    public override bool IsStale => false;

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
}
