using System;
using System.Text.Json;
using System.Threading.Tasks;
using FdwReferenceHost.Logging;
using FdwReferenceHost.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHost.Middleware;

/// <summary>
/// Global exception handler middleware that catches unhandled exceptions and returns
/// a standardized error response with request ID and support contact information.
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly SupportContactInfo _support;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>Creates the middleware with the support contact quoted in error responses.</summary>
    /// <param name="next">The rest of the pipeline.</param>
    /// <param name="logger">The logger for unhandled exceptions.</param>
    /// <param name="support">
    /// The support contact this surface quotes, read from the host's own configuration row once
    /// when the pipeline is composed -- not defaulted here, so a host with no row configured fails
    /// at startup rather than shipping a blank contact block on every 500.
    /// </param>
    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        SupportContactInfo support)
    {
        _next = next;
        _logger = logger;
        _support = support;
    }

    /// <summary>Runs the rest of the pipeline, converting an unhandled exception into an error response.</summary>
    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await HandleException(context, ex).ConfigureAwait(false);
        }
    }

    private Task HandleException(HttpContext context, Exception exception)
    {
        // The id every log line of this request carries as RequestId, so the number a caller quotes
        // finds them. It is the same referenceId every other error from this host returns.
        var referenceId = context.TraceIdentifier;

        HostingLog.UnhandledException(
            _logger,
            exception,
            referenceId,
            context.Request.Path,
            context.Request.Method);

        var errorResponse = new ErrorResponse
        {
            ReferenceId = referenceId,
            Timestamp = DateTimeOffset.UtcNow,
            StatusCode = StatusCodes.Status500InternalServerError,
            Message = "An unexpected error occurred. Quote the reference id when contacting support.",
            Support = _support
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, JsonOptions));
    }
}
