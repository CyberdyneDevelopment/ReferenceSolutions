using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferenceConnections.Endpoints.Logging;

/// <summary>
/// MessageLogging definitions for Connection management endpoints.
/// EventId range: 5200-5299
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class ConnectionLog
{
    // Create operations (5200-5209)
    [MessageLogging(EventId = 5200, Level = LogLevel.Information, Message = "Creating connection '{name}' of type '{connectionType}'")]
    public static partial IGenericMessage CreatingConnection(ILogger logger, string name, string connectionType);

    [MessageLogging(EventId = 5201, Level = LogLevel.Information, Message = "Connection '{name}' created successfully")]
    public static partial IGenericMessage ConnectionCreated(ILogger logger, string name);

    [MessageLogging(EventId = 5202, Level = LogLevel.Error, Message = "Failed to create connection '{name}': {message}")]
    public static partial IGenericMessage ConnectionCreateFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 5203, Level = LogLevel.Warning, Message = "Connection '{name}' already exists")]
    public static partial IGenericMessage ConnectionAlreadyExists(ILogger logger, string name);

    // Read operations (5210-5219)
    [MessageLogging(EventId = 5210, Level = LogLevel.Information, Message = "Fetching connection '{name}'")]
    public static partial IGenericMessage FetchingConnection(ILogger logger, string name);

    [MessageLogging(EventId = 5211, Level = LogLevel.Information, Message = "Connection '{name}' retrieved")]
    public static partial IGenericMessage ConnectionRetrieved(ILogger logger, string name);

    [MessageLogging(EventId = 5212, Level = LogLevel.Warning, Message = "Connection '{name}' not found")]
    public static partial IGenericMessage ConnectionNotFound(ILogger logger, string name);

    [MessageLogging(EventId = 5213, Level = LogLevel.Error, Message = "Failed to fetch connection '{name}': {message}")]
    public static partial IGenericMessage ConnectionFetchFailed(ILogger logger, string name, string message);

    // Update operations (5220-5229)
    [MessageLogging(EventId = 5220, Level = LogLevel.Information, Message = "Updating connection '{name}'")]
    public static partial IGenericMessage UpdatingConnection(ILogger logger, string name);

    [MessageLogging(EventId = 5221, Level = LogLevel.Information, Message = "Connection '{name}' updated successfully")]
    public static partial IGenericMessage ConnectionUpdated(ILogger logger, string name);

    [MessageLogging(EventId = 5222, Level = LogLevel.Error, Message = "Failed to update connection '{name}': {message}")]
    public static partial IGenericMessage ConnectionUpdateFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 5223, Level = LogLevel.Warning, Message = "ETag mismatch for connection '{name}' - concurrent modification detected")]
    public static partial IGenericMessage ConnectionETagMismatch(ILogger logger, string name);

    // Delete operations (5230-5239)
    [MessageLogging(EventId = 5230, Level = LogLevel.Information, Message = "Deleting connection '{name}'")]
    public static partial IGenericMessage DeletingConnection(ILogger logger, string name);

    [MessageLogging(EventId = 5231, Level = LogLevel.Information, Message = "Connection '{name}' deleted successfully")]
    public static partial IGenericMessage ConnectionDeleted(ILogger logger, string name);

    [MessageLogging(EventId = 5232, Level = LogLevel.Error, Message = "Failed to delete connection '{name}': {message}")]
    public static partial IGenericMessage ConnectionDeleteFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 5233, Level = LogLevel.Warning, Message = "Cannot delete connection '{name}' - referenced by {count} pipeline(s): {pipelines}")]
    public static partial IGenericMessage ConnectionInUse(ILogger logger, string name, int count, string pipelines);

    // Validation operations (5240-5249)
    [MessageLogging(EventId = 5240, Level = LogLevel.Warning, Message = "Validation failed for connection '{name}': {message}")]
    public static partial IGenericMessage ConnectionValidationFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 5241, Level = LogLevel.Warning, Message = "Unknown connection type '{connectionType}'")]
    public static partial IGenericMessage UnknownConnectionType(ILogger logger, string connectionType);

    [MessageLogging(EventId = 5242, Level = LogLevel.Warning, Message = "Failed to save authentication for connection '{connectionId}': {error}")]
    public static partial IGenericMessage AuthenticationSaveFailed(ILogger logger, string connectionId, string error);

    // List and test operations (5250-5259)
    [MessageLogging(EventId = 5250, Level = LogLevel.Information, Message = "Listing configured connections")]
    public static partial IGenericMessage ListingConnections(ILogger logger);

    [MessageLogging(EventId = 5251, Level = LogLevel.Information, Message = "Found {count} configured connections")]
    public static partial IGenericMessage ConnectionsListed(ILogger logger, int count);

    [MessageLogging(EventId = 5252, Level = LogLevel.Information, Message = "Testing connection: {name}")]
    public static partial IGenericMessage TestingConnection(ILogger logger, string name);

    [MessageLogging(EventId = 5253, Level = LogLevel.Warning, Message = "Connection test failed for {name}: {message}")]
    public static partial IGenericMessage ConnectionTestFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 5254, Level = LogLevel.Information, Message = "Connection test succeeded for {name}")]
    public static partial IGenericMessage ConnectionTestSucceeded(ILogger logger, string name);

    [MessageLogging(EventId = 5255, Level = LogLevel.Error, Message = "Connection '{name}' has no ServiceOptionType configured")]
    public static partial IGenericMessage ServiceOptionTypeMissing(ILogger logger, string name);

    // Test config operations (5260-5269)
    [MessageLogging(EventId = 5260, Level = LogLevel.Trace, Message = "Testing connection config for '{name}' in-memory")]
    public static partial IGenericMessage TestingConnectionConfig(ILogger logger, string name);

    [MessageLogging(EventId = 5261, Level = LogLevel.Information, Message = "Connection config test succeeded for '{name}'")]
    public static partial IGenericMessage ConnectionConfigTestSucceeded(ILogger logger, string name);

    [MessageLogging(EventId = 5262, Level = LogLevel.Warning, Message = "Connection config test failed for '{name}': {message}")]
    public static partial IGenericMessage ConnectionConfigTestFailed(ILogger logger, string name, string message);

    [MessageLogging(EventId = 5263, Level = LogLevel.Error, Message = "Failed to build connection from config for '{name}': {message}")]
    public static partial IGenericMessage ConnectionConfigBuildFailed(ILogger logger, string name, string message);

    // Capabilities operations (5270-5279)
    [MessageLogging(EventId = 5270, Level = LogLevel.Trace, Message = "Loading capabilities for connection type '{connectionTypeName}'")]
    public static partial IGenericMessage LoadingCapabilities(ILogger logger, string connectionTypeName);

    [MessageLogging(EventId = 5271, Level = LogLevel.Information, Message = "Capabilities loaded for connection type '{connectionTypeName}'")]
    public static partial IGenericMessage CapabilitiesLoaded(ILogger logger, string connectionTypeName);

    [MessageLogging(EventId = 5272, Level = LogLevel.Warning, Message = "Connection type '{connectionTypeName}' not found when loading capabilities")]
    public static partial IGenericMessage CapabilitiesTypeNotFound(ILogger logger, string connectionTypeName);
}
