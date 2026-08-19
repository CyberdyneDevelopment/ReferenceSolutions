using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using ReferenceEndpoints.Logging;
using ReferenceEndpoints.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ReferenceEndpoints.Middleware;

/// <summary>
/// Global exception handler middleware that catches unhandled exceptions and returns
/// a standardized error response with request ID and support contact information.
/// </summary>
public sealed class GlobalExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;
    private readonly SupportOptions _supportOptions;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>Creates the middleware with the support contact quoted in error responses.</summary>
    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger,
        IOptions<SupportOptions> supportOptions)
    {
        _next = next;
        _logger = logger;
        _supportOptions = supportOptions.Value;

        if (string.IsNullOrWhiteSpace(_supportOptions.Email))
        {
            HostingLog.SupportContactNotConfigured(_logger);
        }
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
        var requestId = GetRequestId(context);

        HostingLog.UnhandledException(
            _logger,
            exception,
            requestId,
            context.Request.Path,
            context.Request.Method);

        var errorResponse = new ErrorResponse
        {
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow,
            StatusCode = StatusCodes.Status500InternalServerError,
            Message = "An unexpected error occurred. Please reference the Request ID when contacting support.",
            Support = new SupportContactInfo
            {
                Email = _supportOptions.Email,
                Phone = _supportOptions.Phone,
                PortalUrl = _supportOptions.PortalUrl,
                Instructions = _supportOptions.Instructions,
                ExpectedResponseTimeHours = _supportOptions.ExpectedResponseTimeHours
            }
        };

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";

        return context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, JsonOptions));
    }

    private static string GetRequestId(HttpContext context)
    {
        var activity = Activity.Current;
        if (activity != null && activity.TraceId != default)
        {
            return activity.TraceId.ToString();
        }

        if (!string.IsNullOrEmpty(context.TraceIdentifier))
        {
            return context.TraceIdentifier;
        }

        return Guid.NewGuid().ToString("N");
    }
}
