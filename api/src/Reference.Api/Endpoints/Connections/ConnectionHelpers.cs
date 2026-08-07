using System.Diagnostics.CodeAnalysis;
using System;
using Fdw.Configuration;
using Microsoft.Extensions.Logging;
using Reference.Api.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Resolves the ServiceOptionType from a configuration, logging an error if missing.
/// Derives a fallback from the type name so no hardcoded connection type is used.
/// </summary>
internal static class ConfigServiceType
{
    internal static string Resolve(IGenericConfiguration config, ILogger logger)
    {
        if (!string.IsNullOrEmpty(config.ServiceOptionType))
        {
            return config.ServiceOptionType;
        }

        ConnectionLog.ServiceOptionTypeMissing(logger, config.Name);
        return config.GetType().Name.Replace("Configuration", string.Empty, StringComparison.Ordinal);
    }
}
