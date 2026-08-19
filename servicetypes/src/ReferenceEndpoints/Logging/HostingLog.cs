using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferenceEndpoints.Logging;

/// <summary>
/// MessageLogging for FDW hosting operations.
/// EventId range: 500-550
/// </summary>
[ExcludeFromCodeCoverage(Justification = "MessageLogging partial class - implementation is source-generated")]
[MessageLoggingTypeCode("HOSTING")]
public static partial class HostingLog
{
    [MessageLogging(EventId = 11001, Level = LogLevel.Information, Message = "FDW host starting: {applicationName}")]
    public static partial IGenericMessage HostStarting(ILogger logger, string applicationName);

    [MessageLogging(EventId = 11002, Level = LogLevel.Information, Message = "FDW host started on port {port}")]
    public static partial IGenericMessage HostStarted(ILogger logger, int port);

    // Why: EventIds 502-504 (ControlDbConnecting/Connected/ConnectionFailed) retired with ControlDb purge. Not reused.

    [MessageLogging(EventId = 11003, Level = LogLevel.Information, Message = "ServiceType registration: {phase} phase for {typeName}")]
    public static partial IGenericMessage ServiceTypeRegistrationPhase(ILogger logger, string phase, string typeName);

    [MessageLogging(EventId = 11004, Level = LogLevel.Information, Message = "ServiceType registration complete: {count} types registered")]
    public static partial IGenericMessage ServiceTypeRegistrationComplete(ILogger logger, int count);

    [MessageLogging(EventId = 11005, Level = LogLevel.Information, Message = "Security headers middleware enabled")]
    public static partial IGenericMessage SecurityHeadersEnabled(ILogger logger);

    [MessageLogging(EventId = 11006, Level = LogLevel.Information, Message = "CORS configured with {originCount} allowed origins")]
    public static partial IGenericMessage CorsConfigured(ILogger logger, int originCount);

    [MessageLogging(EventId = 61000, Level = LogLevel.Warning, Message = "CORS using development localhost policy")]
    public static partial IGenericMessage CorsUsingDevPolicy(ILogger logger);

    // Why: EventIds 510-511 (InternalApiKeyEnabled/Disabled) retired with InternalApiKey purge. Not reused.

    [MessageLogging(EventId = 11007, Level = LogLevel.Information, Message = "Health endpoint mapped at /health for {serviceName}")]
    public static partial IGenericMessage HealthEndpointMapped(ILogger logger, string serviceName);

    [MessageLogging(EventId = 11008, Level = LogLevel.Information, Message = "Rate limiting enabled with {policyCount} policies")]
    public static partial IGenericMessage RateLimitingEnabled(ILogger logger, int policyCount);

    // ═══════════════════════════════════════════════════════════════════════════
    // Global Exception Handler (514-516)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 91000, Level = LogLevel.Error, Message = "Unhandled exception. RequestId: {requestId}, Path: {path}, Method: {method}")]
    public static partial IGenericMessage UnhandledException(ILogger logger, Exception ex, string requestId, string path, string method);

    [MessageLogging(EventId = 61001, Level = LogLevel.Warning, Message = "Support contact not configured")]
    public static partial IGenericMessage SupportContactNotConfigured(ILogger logger);

    [MessageLogging(EventId = 11009, Level = LogLevel.Information, Message = "Global exception handler enabled")]
    public static partial IGenericMessage GlobalExceptionHandlerEnabled(ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Startup (540-545)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 91001, Level = LogLevel.Critical, Message = "Startup failed with {failureCount} error(s)")]
    public static partial IGenericMessage StartupFailed(ILogger logger, int failureCount);

    [MessageLogging(EventId = 91002, Level = LogLevel.Error, Message = "  [{phase}] {stepName}: {error}")]
    public static partial IGenericMessage StartupStepFailed(ILogger logger, string phase, string stepName, string error);

    [MessageLogging(EventId = 11010, Level = LogLevel.Information, Message = "Startup completed successfully: {stepCount} steps")]
    public static partial IGenericMessage StartupCompleted(ILogger logger, int stepCount);

    [MessageLogging(EventId = 61002, Level = LogLevel.Warning, Message = "Startup step skipped (fatal dependency): [{phase}] {stepName}")]
    public static partial IGenericMessage StartupStepSkipped(ILogger logger, string phase, string stepName);

    // Why: EventIds 530-532 (JwtConfigurationMissing/JwtSecretKeyEmpty/JwtAuthenticationConfigured)
    // retired with the JWT auth project purge. Not reused.

    // ═══════════════════════════════════════════════════════════════════════════
    // OpenTelemetry (544)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 11011, Level = LogLevel.Information, Message = "OpenTelemetry configured for service '{serviceName}'")]
    public static partial IGenericMessage OpenTelemetryConfigured(ILogger logger, string serviceName);

    // ═══════════════════════════════════════════════════════════════════════════
    // SignalR (546-547)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 11012, Level = LogLevel.Information, Message = "SignalR configured with framework broadcasters")]
    public static partial IGenericMessage SignalRConfigured(ILogger logger);

    [MessageLogging(EventId = 11013, Level = LogLevel.Information, Message = "SignalR hubs mapped: pipelines, configuration")]
    public static partial IGenericMessage SignalRHubsMapped(ILogger logger);

    // ═══════════════════════════════════════════════════════════════════════════
    // Application Pipeline (548)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 11014, Level = LogLevel.Information, Message = "Framework application pipeline configured (multitenancy={multitenancyEnabled})")]
    public static partial IGenericMessage ApplicationPipelineConfigured(ILogger logger, bool multitenancyEnabled);
}
