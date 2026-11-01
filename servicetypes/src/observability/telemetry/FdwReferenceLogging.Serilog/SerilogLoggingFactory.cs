using System;
using System.Globalization;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using FdwReferenceLogging.Serilog.Logging;
using Serilog;
using Serilog.Events;

namespace FdwReferenceLogging.Serilog;

/// <summary>Builds a Serilog pipeline from its configuration.</summary>
public sealed class SerilogLoggingFactory
    : PlatformServiceFactory<ILoggingService, SerilogLoggingConfiguration>, ISerilogLoggingFactory
{
    private readonly ILogger<SerilogLoggingFactory> _logger;

    /// <summary>Initializes a new instance of the <see cref="SerilogLoggingFactory"/> class.</summary>
    /// <param name="loggerFactory">Supplies the factory logger and the logger handed to each built pipeline.</param>
    public SerilogLoggingFactory(ILoggerFactory loggerFactory)
        : base(
            (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)))
                .CreateLogger<SerilogLoggingFactory>(),
            loggerFactory.CreateLogger<ILoggingService>())
        => _logger = loggerFactory.CreateLogger<SerilogLoggingFactory>();

    /// <summary>Builds the Serilog pipeline described by the configuration.</summary>
    /// <param name="configuration">The Serilog settings to build from.</param>
    /// <returns>The created service, or a failure describing why it could not be built.</returns>
    public override IGenericResult<ILoggingService> Create(SerilogLoggingConfiguration configuration)
    {
        if (configuration is null)
        {
            return Failed(SerilogLoggingFactoryLogger.ConfigurationNull(_logger));
        }

        // Why parsed and not defaulted: an unrecognised level is a configuration error. Falling back
        // to Information would silently run production at the wrong verbosity.
        if (!Enum.TryParse<LogEventLevel>(configuration.MinimumLevel, ignoreCase: true, out var level))
        {
            return Failed(SerilogLoggingFactoryLogger.MinimumLevelNotRecognised(
                _logger, configuration.Name, configuration.MinimumLevel));
        }

        _ = SerilogLoggingFactoryLogger.BuildingPipeline(_logger, configuration.Name);

        var settings = new LoggerConfiguration().MinimumLevel.Is(level);

        foreach (var levelOverride in configuration.Overrides)
        {
            if (!Enum.TryParse<LogEventLevel>(levelOverride.Level, ignoreCase: true, out var overrideLevel))
            {
                return Failed(SerilogLoggingFactoryLogger.OverrideLevelNotRecognised(
                    _logger, configuration.Name, levelOverride.SourceContext, levelOverride.Level));
            }

            settings = settings.MinimumLevel.Override(levelOverride.SourceContext, overrideLevel);
        }

        settings = ApplyEnrichers(settings, configuration);
        settings = ApplySinks(settings, configuration);

        _ = SerilogLoggingFactoryLogger.PipelineBuilt(
            _logger, configuration.Name, configuration.MinimumLevel, configuration.Overrides.Count);

        return GenericResult<ILoggingService>.Success(
            new SerilogLoggingService(configuration, settings.CreateLogger()));
    }

    private static LoggerConfiguration ApplyEnrichers(
        LoggerConfiguration settings, SerilogLoggingConfiguration configuration)
    {
        if (configuration.EnrichFromLogContext) settings = settings.Enrich.FromLogContext();
        if (configuration.EnrichWithMachineName) settings = settings.Enrich.WithMachineName();
        if (configuration.EnrichWithEnvironmentName) settings = settings.Enrich.WithEnvironmentName();
        if (configuration.EnrichWithThreadId) settings = settings.Enrich.WithThreadId();
        if (configuration.EnrichWithProcessId) settings = settings.Enrich.WithProcessId();

        return string.IsNullOrWhiteSpace(configuration.ApplicationName)
            ? settings
            : settings.Enrich.WithProperty("Application", configuration.ApplicationName);
    }

    private static LoggerConfiguration ApplySinks(
        LoggerConfiguration settings, SerilogLoggingConfiguration configuration)
    {
        if (configuration.WriteToConsole)
        {
            settings = settings.WriteTo.Console(
                outputTemplate: configuration.OutputTemplate,
                formatProvider: CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(configuration.SeqServerUrl))
        {
            settings = settings.WriteTo.Seq(configuration.SeqServerUrl, formatProvider: CultureInfo.InvariantCulture);
        }

        if (!string.IsNullOrWhiteSpace(configuration.FilePath))
        {
            settings = settings.WriteTo.File(configuration.FilePath, formatProvider: CultureInfo.InvariantCulture);
        }

        // Why this needs its own sink: App Service classifies an entry by the level the sink records,
        // and stdout carries none — every console line reaches Kudu as Information whatever its real
        // severity. This sink writes through System.Diagnostics.Trace, which App Service records WITH
        // the level, so the severity survives. Its default template is left alone deliberately: the
        // level travels structurally, and restating it in the text duplicates what the host stamps.
        return configuration.WriteToAzureAppService
            ? settings.WriteTo.AzureApp(formatProvider: CultureInfo.InvariantCulture)
            : settings;
    }

    private static IGenericResult<ILoggingService> Failed(IGenericMessage message)
        => GenericResult<ILoggingService>.Failure(message);
}
