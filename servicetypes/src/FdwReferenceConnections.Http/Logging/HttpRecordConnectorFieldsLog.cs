using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.Http.Logging;

/// <summary>
/// MessageLogging for HTTP record-connector field-schema failures raised locally by this package —
/// separate from the framework's <c>HttpRecordConnectorLog</c> (Fdw.Services.Connections.Http),
/// which this package does not own.
/// </summary>
[MessageLoggingTypeCode("HTTP")]
public static partial class HttpRecordConnectorFieldsLog
{
    /// <summary>
    /// Logs that a container child node is not an <c>IDataField</c>, so the record writer's field
    /// schema cannot be built.
    /// </summary>
    [MessageLogging(EventId = 61008, Level = LogLevel.Error, Message = "HTTP connection '{connectionName}': container '{container}' child node '{node}' is not an IDataField")]
    public static partial IGenericMessage FieldNotIDataField(ILogger logger, string connectionName, string container, string node);
}
