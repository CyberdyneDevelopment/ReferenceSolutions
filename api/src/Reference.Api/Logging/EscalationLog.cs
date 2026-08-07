using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for Escalation management endpoints.
/// EventId range: 8930-8949
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class EscalationLog
{
    [MessageLogging(EventId = 8930, Level = LogLevel.Information, Message = "Listing escalation policies")]
    public static partial IGenericMessage ListingPolicies(ILogger logger);

    [MessageLogging(EventId = 8931, Level = LogLevel.Information, Message = "Found {count} escalation policies")]
    public static partial IGenericMessage PoliciesFound(ILogger logger, int count);

    [MessageLogging(EventId = 8932, Level = LogLevel.Error, Message = "Failed to list escalation policies: {message}")]
    public static partial IGenericMessage ListPoliciesFailed(ILogger logger, string message);

    [MessageLogging(EventId = 8933, Level = LogLevel.Information, Message = "Fetching escalation policy {policyId}")]
    public static partial IGenericMessage FetchingPolicy(ILogger logger, Guid policyId);

    [MessageLogging(EventId = 8934, Level = LogLevel.Warning, Message = "Escalation policy {policyId} not found")]
    public static partial IGenericMessage PolicyNotFound(ILogger logger, Guid policyId);

    [MessageLogging(EventId = 8935, Level = LogLevel.Error, Message = "Failed to get escalation policy {policyId}: {message}")]
    public static partial IGenericMessage GetPolicyFailed(ILogger logger, Guid policyId, string message);

    [MessageLogging(EventId = 8936, Level = LogLevel.Information, Message = "Creating escalation policy '{name}'")]
    public static partial IGenericMessage CreatingPolicy(ILogger logger, string name);

    [MessageLogging(EventId = 8937, Level = LogLevel.Error, Message = "Failed to create escalation policy: {message}")]
    public static partial IGenericMessage CreatePolicyFailed(ILogger logger, string message);

    [MessageLogging(EventId = 8938, Level = LogLevel.Information, Message = "Updating escalation policy {policyId}")]
    public static partial IGenericMessage UpdatingPolicy(ILogger logger, Guid policyId);

    [MessageLogging(EventId = 8939, Level = LogLevel.Error, Message = "Failed to update escalation policy {policyId}: {message}")]
    public static partial IGenericMessage UpdatePolicyFailed(ILogger logger, Guid policyId, string message);

    [MessageLogging(EventId = 8940, Level = LogLevel.Information, Message = "Deleting escalation policy {policyId}")]
    public static partial IGenericMessage DeletingPolicy(ILogger logger, Guid policyId);

    [MessageLogging(EventId = 8941, Level = LogLevel.Error, Message = "Failed to delete escalation policy {policyId}: {message}")]
    public static partial IGenericMessage DeletePolicyFailed(ILogger logger, Guid policyId, string message);
}
