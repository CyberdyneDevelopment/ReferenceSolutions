using Fdw.Services.Logging.Abstractions;

namespace FdwReferenceLogging.Serilog;

/// <summary>Builds the Serilog logging service from its configuration.</summary>
public interface ISerilogLoggingFactory
    : ILoggingFactory<ILoggingService, SerilogLoggingConfiguration>
{
}
