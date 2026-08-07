using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for Promotion management endpoints.
/// EventId range: 8900-8929
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class PromotionLog
{
    // List operations (8900-8909)
    [MessageLogging(EventId = 8900, Level = LogLevel.Information, Message = "Listing environments")]
    public static partial IGenericMessage ListingEnvironments(ILogger logger);

    [MessageLogging(EventId = 8901, Level = LogLevel.Information, Message = "Found {count} environments")]
    public static partial IGenericMessage EnvironmentsFound(ILogger logger, int count);

    [MessageLogging(EventId = 8902, Level = LogLevel.Error, Message = "Failed to list environments: {message}")]
    public static partial IGenericMessage ListEnvironmentsFailed(ILogger logger, string message);

    [MessageLogging(EventId = 8903, Level = LogLevel.Information, Message = "Listing promotion requests")]
    public static partial IGenericMessage ListingPromotions(ILogger logger);

    [MessageLogging(EventId = 8904, Level = LogLevel.Information, Message = "Found {count} promotion requests")]
    public static partial IGenericMessage PromotionsFound(ILogger logger, int count);

    [MessageLogging(EventId = 8905, Level = LogLevel.Error, Message = "Failed to list promotion requests: {message}")]
    public static partial IGenericMessage ListPromotionsFailed(ILogger logger, string message);

    // Get operations (8910-8914)
    [MessageLogging(EventId = 8910, Level = LogLevel.Information, Message = "Fetching promotion request {requestId}")]
    public static partial IGenericMessage FetchingPromotion(ILogger logger, Guid requestId);

    [MessageLogging(EventId = 8911, Level = LogLevel.Warning, Message = "Promotion request {requestId} not found")]
    public static partial IGenericMessage PromotionNotFound(ILogger logger, Guid requestId);

    [MessageLogging(EventId = 8912, Level = LogLevel.Error, Message = "Failed to get promotion request {requestId}: {message}")]
    public static partial IGenericMessage GetPromotionFailed(ILogger logger, Guid requestId, string message);

    // Create operations (8915-8919)
    [MessageLogging(EventId = 8915, Level = LogLevel.Information, Message = "Creating promotion request: {sourceEnvironment} to {targetEnvironment} by '{requestedBy}'")]
    public static partial IGenericMessage CreatingPromotion(ILogger logger, string sourceEnvironment, string targetEnvironment, string requestedBy);

    [MessageLogging(EventId = 8916, Level = LogLevel.Error, Message = "Failed to create promotion request: {message}")]
    public static partial IGenericMessage CreatePromotionFailed(ILogger logger, string message);

    // Approve/Reject operations (8920-8924)
    [MessageLogging(EventId = 8920, Level = LogLevel.Information, Message = "Approving promotion request {requestId} by '{approvedBy}'")]
    public static partial IGenericMessage ApprovingPromotion(ILogger logger, Guid requestId, string approvedBy);

    [MessageLogging(EventId = 8921, Level = LogLevel.Error, Message = "Failed to approve promotion request {requestId}: {message}")]
    public static partial IGenericMessage ApprovePromotionFailed(ILogger logger, Guid requestId, string message);

    [MessageLogging(EventId = 8922, Level = LogLevel.Information, Message = "Rejecting promotion request {requestId} by '{rejectedBy}'")]
    public static partial IGenericMessage RejectingPromotion(ILogger logger, Guid requestId, string rejectedBy);

    [MessageLogging(EventId = 8923, Level = LogLevel.Error, Message = "Failed to reject promotion request {requestId}: {message}")]
    public static partial IGenericMessage RejectPromotionFailed(ILogger logger, Guid requestId, string message);

    // Execute operations (8925-8926)
    [MessageLogging(EventId = 8925, Level = LogLevel.Information, Message = "Executing promotion request {requestId}")]
    public static partial IGenericMessage ExecutingPromotion(ILogger logger, Guid requestId);

    [MessageLogging(EventId = 8926, Level = LogLevel.Error, Message = "Failed to execute promotion request {requestId}: {message}")]
    public static partial IGenericMessage ExecutePromotionFailed(ILogger logger, Guid requestId, string message);

    // Compare operations (8927-8929)
    [MessageLogging(EventId = 8927, Level = LogLevel.Information, Message = "Comparing environments '{sourceEnvironment}' vs '{targetEnvironment}' for {entityType} '{entityName}'")]
    public static partial IGenericMessage ComparingEnvironments(ILogger logger, string sourceEnvironment, string targetEnvironment, string entityType, string entityName);

    [MessageLogging(EventId = 8928, Level = LogLevel.Error, Message = "Failed to compare environments '{sourceEnvironment}' vs '{targetEnvironment}': {message}")]
    public static partial IGenericMessage CompareEnvironmentsFailed(ILogger logger, string sourceEnvironment, string targetEnvironment, string message);
}
