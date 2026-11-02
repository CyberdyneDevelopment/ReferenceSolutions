using Fdw.Services.Abstractions;

using Fdw.Services.Logging.Abstractions;
namespace FdwReferenceLogging.Serilog;

/// <summary>Supplies the SerilogLogging implementation's own configuration.</summary>
public interface ISerilogLoggingConfigurationProvider
    : IImplementationConfigurationProvider<ILoggingImplementationConfiguration>
{
}
