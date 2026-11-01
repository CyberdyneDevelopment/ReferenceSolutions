using Fdw.Services.Abstractions;

using Fdw.Services.Telemetry.Abstractions;
namespace FdwReferenceTelemetry.OpenTelemetry;

/// <summary>Supplies the OpenTelemetry implementation's own configuration.</summary>
public interface IOpenTelemetryConfigurationProvider
    : IImplementationConfigurationProvider<ITelemetryImplementationConfiguration>
{
}
