using Fdw.Services.Telemetry.Abstractions;

namespace FdwReferenceTelemetry.OpenTelemetry;

/// <summary>Builds the telemetry pipeline service from its configuration.</summary>
public interface IOpenTelemetryFactory
    : ITelemetryFactory<ITelemetryService, OpenTelemetryConfiguration>
{
}
