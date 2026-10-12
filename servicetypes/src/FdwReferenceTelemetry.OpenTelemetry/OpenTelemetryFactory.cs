using System;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.Telemetry.Abstractions;
using Microsoft.Extensions.Logging;
using FdwReferenceTelemetry.OpenTelemetry.Logging;

namespace FdwReferenceTelemetry.OpenTelemetry;

/// <summary>Builds the telemetry pipeline service from its configuration.</summary>
public sealed class OpenTelemetryFactory
    : PlatformServiceFactory<ITelemetryService, OpenTelemetryConfiguration>, IOpenTelemetryFactory
{
    private readonly ILogger<OpenTelemetryFactory> _logger;

    /// <summary>Initializes a new instance of the <see cref="OpenTelemetryFactory"/> class.</summary>
    /// <param name="loggerFactory">Supplies the factory logger and the logger handed to each host.</param>
    public OpenTelemetryFactory(ILoggerFactory loggerFactory)
        : base(
            (loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory)))
                .CreateLogger<OpenTelemetryFactory>(),
            loggerFactory.CreateLogger<ITelemetryService>())
        => _logger = loggerFactory.CreateLogger<OpenTelemetryFactory>();

    /// <summary>Creates the host service.</summary>
    /// <param name="configuration">The pipeline settings to build from.</param>
    /// <returns>The created service, or a failure describing why it could not be built.</returns>
    public override IGenericResult<ITelemetryService> Create(OpenTelemetryConfiguration configuration)
    {
        if (configuration is null)
        {
            return GenericResult<ITelemetryService>.Failure(
                OpenTelemetryFactoryLogger.ConfigurationNull(_logger));
        }

        _ = OpenTelemetryFactoryLogger.HostResolved(
            _logger, configuration.Name, configuration.ServiceName);

        return GenericResult<ITelemetryService>.Success(new OpenTelemetryService(configuration));
    }
}
