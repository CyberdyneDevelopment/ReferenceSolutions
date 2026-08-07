using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for SecretManager endpoint operations.
/// EventId range: 8500-8549
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class SecretManagerLog
{
    [MessageLogging(EventId = 8500, Level = LogLevel.Information, Message = "Listing secret managers")]
    public static partial IGenericMessage ListingSecretManagers(ILogger logger);

    [MessageLogging(EventId = 8501, Level = LogLevel.Information, Message = "Found {count} secret manager(s)")]
    public static partial IGenericMessage SecretManagersListed(ILogger logger, int count);

    [MessageLogging(EventId = 8502, Level = LogLevel.Information, Message = "Getting secret manager '{name}'")]
    public static partial IGenericMessage GettingSecretManager(ILogger logger, string name);

    [MessageLogging(EventId = 8503, Level = LogLevel.Warning, Message = "Secret manager '{name}' not found")]
    public static partial IGenericMessage SecretManagerNotFound(ILogger logger, string name);
}
