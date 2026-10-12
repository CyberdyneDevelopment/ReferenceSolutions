using System;
﻿using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Logging;
using Fdw.Services.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.Generic;
using System.Globalization;
using global::Serilog;
using global::Serilog.Extensions.Hosting;
namespace FdwReferenceLogging.Serilog.Registration;

/// <summary>
/// The Serilog logging option: builds the application's Serilog pipeline from configuration held in
/// the server tier.
/// </summary>
/// <remarks>
/// This replaces the <c>builder.Host.UseSerilog(… ReadFrom.Configuration(context.Configuration) …)</c>
/// an application would otherwise make, and the literals in its stage-1 bootstrap logger. That
/// bootstrap logger itself stays — it exists to log before any configuration source is reachable,
/// including this one.
/// </remarks>
[Implementation(typeof(LoggingTypes), "Serilog")]
public sealed class SerilogLoggingType
    : LoggingTypeBase<ILoggingService, SerilogLoggingConfiguration, ISerilogLoggingFactory>
{
    /// <summary>The domain whose row this option configures.</summary>
    /// <remarks>
    /// A domain provider's <c>Get(name)</c> takes the DOMAIN row's Name, and that row is named for
    /// the domain it configures — "Logging", carrying Implementation "Serilog". This option's own
    /// Name selects the implementation, not the row.
    /// </remarks>
    private const string DomainName = "Logging";

    /// <summary>Initializes a new instance of the <see cref="SerilogLoggingType"/> class.</summary>
    public SerilogLoggingType()
        : base(
            name: "Serilog",
            sectionName: "Logging",
            displayName: "Serilog",
            description: "Serilog pipeline configured from the server configuration tier")
    {
        // The bootstrap logger. It exists to log before any configuration source is reachable,
        // including this option's own rows, so it is console-only and its literals are the only
        // ones in this package. A host used to write this block itself.
        // Why a bootstrap (reloadable) logger and not CreateLogger: a host hands this logger to the
        // startup LoggerFactory, and the registration bodies keep loggers made from it for the life
        // of the process (the configuration gateway's result cache, for one). A plain logger left
        // those writing in this format, without the request's RequestId, after Initialize had put the
        // configured pipeline in place. A reloadable one is redirected when Initialize reloads it.
        Configuration(builder =>
        {
            global::Serilog.Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithEnvironmentName()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
                    formatProvider: CultureInfo.InvariantCulture)
                .CreateBootstrapLogger();

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Registration((builder, loggerFactory) =>
        {
            builder.Services.TryAdd(new ServiceDescriptor(
                typeof(ISerilogLoggingFactory), typeof(SerilogLoggingFactory), ServiceLifetime.Singleton));

            // Why NullLogger and not ILoggerFactory: this provider is reachable from the AddSerilog
            // callback below, which runs DURING logger construction. Asking the container for a
            // logger here would re-enter that callback -- the cycle that leaves the host parked
            // forever rather than throwing, because DI's StackGuard turns the recursion into
            // cross-thread waits. Nothing inside logger construction may resolve a logger.
            builder.Services.AddSingleton<ISerilogLoggingConfigurationProvider, SerilogLoggingConfigurationProvider>(sp => new SerilogLoggingConfigurationProvider(
                NullLogger<SerilogLoggingConfigurationProvider>.Instance,
                sp.GetRequiredService<IConfigurationGatewayProvider>(),
                LoggingTypes.ConfigurationConnection, "log", "SerilogLoggingConfiguration"));

            // Wire the host to Serilog's static logger and nothing else. No callback that resolves
            // anything: the bootstrap logger set in Configure serves until Initialize replaces
            // Log.Logger with the configured pipeline. A callback that reached for configuration
            // here re-entered the configuration gateway, whose own connection wants a logger, and
            // DI turned that recursion into cross-thread waits -- the host parked with no exception
            // and no log line.
            builder.Services.AddSerilog();

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Why Initialize: this needs a LIVE container. The implementation configuration provider is
        // registered with the domain provider here, so the row is only readable once that has run.
        Initialization((host, loggerFactory) =>
        {
            var services = host.Services;
            var log = loggerFactory?.CreateLogger<SerilogLoggingType>()
                ?? NullLogger<SerilogLoggingType>.Instance;

            var configResult = services.GetRequiredService<ILoggingConfigurationProvider>()
                .Register(Name, services.GetRequiredService<ISerilogLoggingConfigurationProvider>());
            if (!configResult.IsSuccess)
                return configResult.ToNewResult<IHost>();

            // Read the row and configure Serilog from it -- the same thing an application would
            // write in Program.cs, moved here so it is configuration rather than literals. There is
            // no factory and no service: a logging pipeline is configuration applied to the host's
            // logger, not an object something resolves and sends commands to.
#pragma warning disable VSTHRD002
            var row = services.GetRequiredService<ILoggingConfigurationProvider>()
                .Get(DomainName).GetAwaiter().GetResult();
#pragma warning restore VSTHRD002
            if (row.IsFailure || row.Value is null)
                return GenericResult<IHost>.Failure(
                    SerilogLoggingLog.LoggingRowNotConfigured(log, DomainName));

            if (row.Value is not SerilogLoggingConfiguration serilogRow)
                return GenericResult<IHost>.Failure(
                    SerilogLoggingLog.LoggerNotBuiltFromRow(
                        log, DomainName,
                        FormattableString.Invariant(
                            $"the '{DomainName}' row is a {row.Value.GetType().Name}, not a {nameof(SerilogLoggingConfiguration)}")));

            if (!Enum.TryParse<global::Serilog.Events.LogEventLevel>(
                    serilogRow.MinimumLevel, ignoreCase: true, out var level))
                return GenericResult<IHost>.Failure(
                    SerilogLoggingLog.LoggerNotBuiltFromRow(
                        log, DomainName,
                        FormattableString.Invariant($"MinimumLevel '{serilogRow.MinimumLevel}' is not a Serilog level")));

            var overrides = new List<(string SourceContext, global::Serilog.Events.LogEventLevel Level)>();

            foreach (var levelOverride in serilogRow.Overrides)
            {
                if (!Enum.TryParse<global::Serilog.Events.LogEventLevel>(
                        levelOverride.Level, ignoreCase: true, out var overrideLevel))
                    return GenericResult<IHost>.Failure(
                        SerilogLoggingLog.LoggerNotBuiltFromRow(
                            log, DomainName,
                            FormattableString.Invariant(
                                $"override level '{levelOverride.Level}' for '{levelOverride.SourceContext}' is not a Serilog level")));

                overrides.Add((levelOverride.SourceContext, overrideLevel));
            }

            if (global::Serilog.Log.Logger is not ReloadableLogger bootstrap)
                return GenericResult<IHost>.Failure(
                    SerilogLoggingLog.LoggerNotBuiltFromRow(
                        log, DomainName,
                        "the logger in place is not the reloadable bootstrap logger Configure sets, so loggers made before this point would keep writing to it"));

            // Reloading rather than replacing Log.Logger: every logger already made from the
            // bootstrap logger -- the host's startup LoggerFactory hands it to every registration
            // body -- is redirected to the configured pipeline, not just the ones made from now on.
            // Freeze then fixes the pipeline so those loggers stop paying for the indirection.
            bootstrap.Reload(settings => ConfiguredPipeline(settings, level, overrides, serilogRow));
            bootstrap.Freeze();

            return GenericResult<IHost>.Success(host);
        });
    }

    private static LoggerConfiguration ConfiguredPipeline(
        LoggerConfiguration settings,
        global::Serilog.Events.LogEventLevel level,
        IReadOnlyList<(string SourceContext, global::Serilog.Events.LogEventLevel Level)> overrides,
        SerilogLoggingConfiguration configuration)
    {
        settings = settings.MinimumLevel.Is(level);
        foreach (var levelOverride in overrides)
            settings = settings.MinimumLevel.Override(levelOverride.SourceContext, levelOverride.Level);

        return ApplySinks(ApplyEnrichers(settings, configuration), configuration);
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
            settings = settings.WriteTo.Seq(configuration.SeqServerUrl, formatProvider: CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(configuration.FilePath))
            settings = settings.WriteTo.File(configuration.FilePath, formatProvider: CultureInfo.InvariantCulture);

        // Why this needs its own sink: App Service classifies an entry by the level the sink records,
        // and stdout carries none — every console line reaches Kudu as Information whatever its real
        // severity. This sink writes through System.Diagnostics.Trace, which App Service records WITH
        // the level, so the severity survives.
        return configuration.WriteToAzureAppService
            ? settings.WriteTo.AzureApp(formatProvider: CultureInfo.InvariantCulture)
            : settings;
    }
}