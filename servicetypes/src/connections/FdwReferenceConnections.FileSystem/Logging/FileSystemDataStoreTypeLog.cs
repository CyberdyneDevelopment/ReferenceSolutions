using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.FileSystem.Logging;

/// <summary>
/// MessageLogging for the FileSystem data-store type registration.
/// EventId range: 9590-9599
/// </summary>
[MessageLoggingTypeCode("FILESYSTEM")]
public static partial class FileSystemDataStoreTypeLog
{
    /// <summary>
    /// Logs that the FileSystem data-store factory and typed config provider were registered.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="dataStoreTypeName">The data-store type name that was registered.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "FileSystem data-store type '{dataStoreTypeName}' registered factory and typed config provider")]
    public static partial IGenericMessage RegistrationCompleted(ILogger logger, string dataStoreTypeName);

    /// <summary>
    /// Logs that the FileSystem data-store factory registration returned a failure result.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="dataStoreTypeName">The data-store type name whose factory registration failed.</param>
    /// <param name="message">The failure message from the registration result.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 61002,
        Level = LogLevel.Warning,
        Message = "FileSystem data-store type '{dataStoreTypeName}' factory registration returned failure: {message}")]
    public static partial IGenericMessage FactoryRegistrationFailed(ILogger logger, string dataStoreTypeName, string? message);

    /// <summary>
    /// Logs that an optional child config provider was not registered, so the FileSystem config
    /// provider will leave the corresponding child collection empty.
    /// </summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="dataStoreTypeName">The data-store type name being registered.</param>
    /// <param name="childProvider">The optional child provider role that was not registered (dataPath/policy/handlerOverride).</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(
        EventId = 11003,
        Level = LogLevel.Debug,
        Message = "FileSystem data-store type '{dataStoreTypeName}': optional child provider '{childProvider}' not registered; child collection left empty")]
    public static partial IGenericMessage ChildProviderNotRegistered(ILogger logger, string dataStoreTypeName, string childProvider);
}
