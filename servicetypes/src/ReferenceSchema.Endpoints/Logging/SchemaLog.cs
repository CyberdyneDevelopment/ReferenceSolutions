using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferenceSchema.Endpoints.Logging;

/// <summary>
/// MessageLogging definitions for Schema discovery endpoints.
/// EventId range: 5500-5599
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class SchemaLog
{
    // Discovery operations (5500-5519)
    [MessageLogging(EventId = 5500, Level = LogLevel.Trace, Message = "Discovering schema for connection '{connectionName}'")]
    public static partial IGenericMessage DiscoveringSchema(ILogger logger, string connectionName);

    [MessageLogging(EventId = 5501, Level = LogLevel.Information, Message = "Schema discovery completed for '{connectionName}' - found {schemaCount} schemas")]
    public static partial IGenericMessage SchemaDiscoveryCompleted(ILogger logger, string connectionName, int schemaCount);

    [MessageLogging(EventId = 5502, Level = LogLevel.Error, Message = "Schema discovery failed for '{connectionName}': {message}")]
    public static partial IGenericMessage SchemaDiscoveryFailed(ILogger logger, string connectionName, string message);

    [MessageLogging(EventId = 5503, Level = LogLevel.Warning, Message = "Connection '{connectionName}' not found for schema discovery")]
    public static partial IGenericMessage SchemaConnectionNotFound(ILogger logger, string connectionName);

    // Connection listing operations (5540-5549)
    [MessageLogging(EventId = 5540, Level = LogLevel.Information, Message = "Listing schema-capable connections")]
    public static partial IGenericMessage ListingSchemaCapableConnections(ILogger logger);

    [MessageLogging(EventId = 5541, Level = LogLevel.Information, Message = "Found {count} schema-capable connections")]
    public static partial IGenericMessage SchemaCapableConnectionsFound(ILogger logger, int count);

    // Validation (5550-5559)
    [MessageLogging(EventId = 5550, Level = LogLevel.Warning, Message = "Invalid preview request: {message}")]
    public static partial IGenericMessage InvalidPreviewRequest(ILogger logger, string message);
}
