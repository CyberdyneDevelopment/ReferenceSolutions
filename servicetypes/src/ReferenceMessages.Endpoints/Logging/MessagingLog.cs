using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace ReferenceMessages.Endpoints.Logging;

/// <summary>
/// MessageLogging definitions for Messaging endpoints.
/// EventId range: 8800-8899
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class MessagingLog
{
    // ═══════════════════════════════════════════════════════════════════════════
    // Message read operations (8800-8819)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8800, Level = LogLevel.Information, Message = "Listing messages for user '{userId}'")]
    public static partial IGenericMessage ListingMessages(ILogger logger, string userId);

    [MessageLogging(EventId = 8801, Level = LogLevel.Information, Message = "Found {count} messages for user '{userId}'")]
    public static partial IGenericMessage MessagesListed(ILogger logger, int count, string userId);

    [MessageLogging(EventId = 8802, Level = LogLevel.Error, Message = "Failed to list messages for user '{userId}': {message}")]
    public static partial IGenericMessage MessageListFailed(ILogger logger, string userId, string message);

    [MessageLogging(EventId = 8803, Level = LogLevel.Information, Message = "Fetching message '{messageId}'")]
    public static partial IGenericMessage FetchingMessage(ILogger logger, string messageId);

    [MessageLogging(EventId = 8804, Level = LogLevel.Information, Message = "Message '{messageId}' retrieved")]
    public static partial IGenericMessage MessageRetrieved(ILogger logger, string messageId);

    [MessageLogging(EventId = 8805, Level = LogLevel.Error, Message = "Failed to fetch message '{messageId}': {message}")]
    public static partial IGenericMessage MessageFetchFailed(ILogger logger, string messageId, string message);

    [MessageLogging(EventId = 8806, Level = LogLevel.Information, Message = "Getting unread count for user '{userId}'")]
    public static partial IGenericMessage GettingUnreadCount(ILogger logger, string userId);

    [MessageLogging(EventId = 8807, Level = LogLevel.Information, Message = "Unread count for user '{userId}': {count}")]
    public static partial IGenericMessage UnreadCountRetrieved(ILogger logger, string userId, int count);

    [MessageLogging(EventId = 8808, Level = LogLevel.Error, Message = "Failed to get unread count for user '{userId}': {message}")]
    public static partial IGenericMessage UnreadCountFailed(ILogger logger, string userId, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Message state transitions (8820-8839)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8820, Level = LogLevel.Information, Message = "Marking message '{messageId}' as read")]
    public static partial IGenericMessage MarkingMessageRead(ILogger logger, string messageId);

    [MessageLogging(EventId = 8821, Level = LogLevel.Information, Message = "Message '{messageId}' marked as read")]
    public static partial IGenericMessage MessageMarkedRead(ILogger logger, string messageId);

    [MessageLogging(EventId = 8822, Level = LogLevel.Error, Message = "Failed to mark message '{messageId}' as read: {message}")]
    public static partial IGenericMessage MarkReadFailed(ILogger logger, string messageId, string message);

    [MessageLogging(EventId = 8823, Level = LogLevel.Information, Message = "Dismissing message '{messageId}'")]
    public static partial IGenericMessage DismissingMessage(ILogger logger, string messageId);

    [MessageLogging(EventId = 8824, Level = LogLevel.Information, Message = "Message '{messageId}' dismissed")]
    public static partial IGenericMessage MessageDismissed(ILogger logger, string messageId);

    [MessageLogging(EventId = 8825, Level = LogLevel.Error, Message = "Failed to dismiss message '{messageId}': {message}")]
    public static partial IGenericMessage DismissFailed(ILogger logger, string messageId, string message);

    [MessageLogging(EventId = 8826, Level = LogLevel.Information, Message = "Archiving message '{messageId}'")]
    public static partial IGenericMessage ArchivingMessage(ILogger logger, string messageId);

    [MessageLogging(EventId = 8827, Level = LogLevel.Information, Message = "Message '{messageId}' archived")]
    public static partial IGenericMessage MessageArchived(ILogger logger, string messageId);

    [MessageLogging(EventId = 8828, Level = LogLevel.Error, Message = "Failed to archive message '{messageId}': {message}")]
    public static partial IGenericMessage ArchiveFailed(ILogger logger, string messageId, string message);

    [MessageLogging(EventId = 8829, Level = LogLevel.Information, Message = "Marking all messages as read for user '{userId}'")]
    public static partial IGenericMessage MarkingAllRead(ILogger logger, string userId);

    [MessageLogging(EventId = 8830, Level = LogLevel.Information, Message = "All messages marked as read for user '{userId}'")]
    public static partial IGenericMessage AllMessagesMarkedRead(ILogger logger, string userId);

    [MessageLogging(EventId = 8831, Level = LogLevel.Error, Message = "Failed to mark all messages as read for user '{userId}': {message}")]
    public static partial IGenericMessage MarkAllReadFailed(ILogger logger, string userId, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Access request operations (8840-8869)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8840, Level = LogLevel.Information, Message = "Creating access request for resource '{resource}' permission '{permission}'")]
    public static partial IGenericMessage CreatingAccessRequest(ILogger logger, string resource, string permission);

    [MessageLogging(EventId = 8841, Level = LogLevel.Information, Message = "Access request created with id '{requestId}'")]
    public static partial IGenericMessage AccessRequestCreated(ILogger logger, string requestId);

    [MessageLogging(EventId = 8842, Level = LogLevel.Error, Message = "Failed to create access request: {message}")]
    public static partial IGenericMessage AccessRequestCreateFailed(ILogger logger, string message);

    [MessageLogging(EventId = 8843, Level = LogLevel.Information, Message = "Listing access requests for user '{userId}'")]
    public static partial IGenericMessage ListingAccessRequests(ILogger logger, string userId);

    [MessageLogging(EventId = 8844, Level = LogLevel.Information, Message = "Found {count} access requests")]
    public static partial IGenericMessage AccessRequestsListed(ILogger logger, int count);

    [MessageLogging(EventId = 8845, Level = LogLevel.Error, Message = "Failed to list access requests: {message}")]
    public static partial IGenericMessage AccessRequestListFailed(ILogger logger, string message);

    [MessageLogging(EventId = 8846, Level = LogLevel.Information, Message = "Approving access request '{requestId}'")]
    public static partial IGenericMessage ApprovingAccessRequest(ILogger logger, string requestId);

    [MessageLogging(EventId = 8847, Level = LogLevel.Information, Message = "Access request '{requestId}' approved")]
    public static partial IGenericMessage AccessRequestApproved(ILogger logger, string requestId);

    [MessageLogging(EventId = 8848, Level = LogLevel.Error, Message = "Failed to approve access request '{requestId}': {message}")]
    public static partial IGenericMessage AccessRequestApproveFailed(ILogger logger, string requestId, string message);

    [MessageLogging(EventId = 8849, Level = LogLevel.Information, Message = "Denying access request '{requestId}'")]
    public static partial IGenericMessage DenyingAccessRequest(ILogger logger, string requestId);

    [MessageLogging(EventId = 8850, Level = LogLevel.Information, Message = "Access request '{requestId}' denied")]
    public static partial IGenericMessage AccessRequestDenied(ILogger logger, string requestId);

    [MessageLogging(EventId = 8851, Level = LogLevel.Error, Message = "Failed to deny access request '{requestId}': {message}")]
    public static partial IGenericMessage AccessRequestDenyFailed(ILogger logger, string requestId, string message);

    // ═══════════════════════════════════════════════════════════════════════════
    // Auth/claims (8870-8879)
    // ═══════════════════════════════════════════════════════════════════════════

    [MessageLogging(EventId = 8870, Level = LogLevel.Warning, Message = "User identity claim not found in request")]
    public static partial IGenericMessage UserClaimNotFound(ILogger logger);
}
