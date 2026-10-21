using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceConnections.FileSystem.Logging;

/// <summary>
/// MessageLogging for the reference retained documents over an organization's document store (FDW-1575).
/// EventId ranges: 13200 (done), 23200-23201 (bad request, 400).
/// </summary>
[MessageLoggingTypeCode("FILESYSTEM")]
public static partial class OrganizationStoreRetainedDocumentsLog
{
    /// <summary>Logs that a document was ended in the organization's store.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="documentId">The document's path in the store.</param>
    /// <param name="endAction">The action performed.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 13200, Level = LogLevel.Information, Message = "Document '{documentId}' was ended in the organization's store with '{endAction}'")]
    public static partial IGenericMessage DocumentEnded(ILogger logger, string documentId, string endAction);

    /// <summary>Logs that the end action is not one this reference application performs.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="endAction">The action the policy names.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 23200, Level = LogLevel.Warning, Message = "The end action '{endAction}' is not one this application performs; it performs Delete and Archive")]
    public static partial IGenericMessage EndActionUnknown(ILogger logger, string endAction);

    /// <summary>Logs that a document type or document id is not a path in the store.</summary>
    /// <param name="logger">The logger used to emit the log event.</param>
    /// <param name="text">The text that is not a path.</param>
    /// <param name="reason">Why it is not a path.</param>
    /// <returns>The structured <see cref="IGenericMessage"/> for the event.</returns>
    [MessageLogging(EventId = 23201, Level = LogLevel.Warning, Message = "'{text}' is not a path in the organization's store: {reason}")]
    public static partial IGenericMessage NotAPath(ILogger logger, string text, string? reason);
}
