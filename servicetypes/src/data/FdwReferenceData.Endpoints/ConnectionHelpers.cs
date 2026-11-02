using System;
using Fdw.Configuration;
using Microsoft.Extensions.Logging;
using FdwReferenceConnections.Endpoints.Logging;

namespace FdwReferenceConnections.Endpoints;

/// <summary>
/// Resolves the Implementation from a configuration, logging an error if missing.
/// Derives a fallback from the type name so no hardcoded connection type is used.
/// </summary>
internal static class ConfigServiceType
{
    internal static string Resolve(IImplementationConfiguration config, ILogger logger)
    {
        if (!string.IsNullOrEmpty(config.Implementation))
        {
            return config.Implementation;
        }

        ConnectionLog.ImplementationMissing(logger, config.Name);
        return config.GetType().Name.Replace("Configuration", string.Empty, StringComparison.Ordinal);
    }
}
