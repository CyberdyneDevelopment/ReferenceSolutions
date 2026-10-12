using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceLogging.Serilog.Logging;

/// <summary>
/// Structured log messages for <see cref="SerilogLoggingFactory"/>. EventIds 62001-62005.
/// </summary>
public static partial class SerilogLoggingFactoryLogger
{
    /// <summary>Logs that the supplied configuration was null.</summary>
    /// <param name="logger">The logger.</param>
    /// <returns>The message describing the failure.</returns>
    [MessageLogging(EventId = 62001, Level = LogLevel.Error,
        Message = "Serilog logging configuration was null")]
    public static partial IGenericMessage ConfigurationNull(ILogger<SerilogLoggingFactory> logger);

    /// <summary>Logs that a Serilog pipeline is being built.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The configuration name.</param>
    /// <returns>The message describing the step.</returns>
    [MessageLogging(EventId = 62002, Level = LogLevel.Debug,
        Message = "Building Serilog pipeline '{configurationName}'")]
    public static partial IGenericMessage BuildingPipeline(
        ILogger<SerilogLoggingFactory> logger, string configurationName);

    /// <summary>Logs that a Serilog pipeline was built.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The configuration name.</param>
    /// <param name="minimumLevel">The level the pipeline emits at.</param>
    /// <param name="overrideCount">How many per-source overrides were applied.</param>
    /// <returns>The message describing the outcome.</returns>
    [MessageLogging(EventId = 62003, Level = LogLevel.Information,
        Message = "Serilog pipeline '{configurationName}' built at level {minimumLevel} with {overrideCount} override(s)")]
    public static partial IGenericMessage PipelineBuilt(
        ILogger<SerilogLoggingFactory> logger, string configurationName, string minimumLevel, int overrideCount);

    /// <summary>Logs that the configured default minimum level is not a Serilog level.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The configuration name.</param>
    /// <param name="minimumLevel">The value that failed to parse.</param>
    /// <returns>The message describing the failure.</returns>
    [MessageLogging(EventId = 62004, Level = LogLevel.Error,
        Message = "Serilog configuration '{configurationName}' has an unrecognised MinimumLevel '{minimumLevel}'")]
    public static partial IGenericMessage MinimumLevelNotRecognised(
        ILogger<SerilogLoggingFactory> logger, string configurationName, string minimumLevel);

    /// <summary>Logs that a per-source override level is not a Serilog level.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="configurationName">The configuration name.</param>
    /// <param name="sourceContext">The source the override applies to.</param>
    /// <param name="level">The value that failed to parse.</param>
    /// <returns>The message describing the failure.</returns>
    [MessageLogging(EventId = 62005, Level = LogLevel.Error,
        Message = "Serilog configuration '{configurationName}' override for '{sourceContext}' has an unrecognised level '{level}'")]
    public static partial IGenericMessage OverrideLevelNotRecognised(
        ILogger<SerilogLoggingFactory> logger, string configurationName, string sourceContext, string level);
}
