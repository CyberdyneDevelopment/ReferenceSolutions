using System.Diagnostics.CodeAnalysis;
using System;
using Microsoft.Extensions.Logging;
using Fdw.Messages;
using Fdw.MessageLogging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging class for application startup operations.
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class ProgramLog
{
    // ========================================================================
    // Application Lifecycle (EventId 1000-1099)
    // ========================================================================

    [MessageLogging(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Starting Reference.Api")]
    public static partial IGenericMessage ApplicationStarting(ILogger logger);

    [MessageLogging(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Reference.Api started successfully")]
    public static partial IGenericMessage ApplicationStarted(ILogger logger);

    [MessageLogging(
        EventId = 1002,
        Level = LogLevel.Critical,
        Message = "Application terminated unexpectedly: {error}")]
    public static partial IGenericMessage ApplicationTerminated(ILogger logger, Exception ex, string error);

    [MessageLogging(
        EventId = 1206,
        Level = LogLevel.Critical,
        Message = "InternalApi:ApiKey must be changed from the development default in non-Development environments")]
    public static partial IGenericMessage InternalApiKeyNotConfigured(ILogger logger);

    // ========================================================================
    // Service Information (EventId 1300-1399)
    // ========================================================================

    [MessageLogging(
        EventId = 1300,
        Level = LogLevel.Information,
        Message = "Scalar UI available at: https://localhost:5001/scalar")]
    public static partial IGenericMessage ScalarAvailable(ILogger logger);

    [MessageLogging(
        EventId = 1301,
        Level = LogLevel.Information,
        Message = "Seq UI available at: {url}")]
    public static partial IGenericMessage SeqAvailable(ILogger logger, string url);

    // ========================================================================
    // Security Configuration (EventId 1302-1399)
    // ========================================================================

    [MessageLogging(
        EventId = 1302,
        Level = LogLevel.Information,
        Message = "CORS configured with {originCount} allowed origins")]
    public static partial IGenericMessage CorsConfigured(ILogger logger, int originCount);

    [MessageLogging(
        EventId = 1303,
        Level = LogLevel.Information,
        Message = "Security headers middleware enabled")]
    public static partial IGenericMessage SecurityHeadersEnabled(ILogger logger);
}
