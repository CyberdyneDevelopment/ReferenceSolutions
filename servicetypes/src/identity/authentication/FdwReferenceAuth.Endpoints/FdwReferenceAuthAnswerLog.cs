using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>The coded messages the fdwreferenceauth endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("RSTAUTHANSWERS")]
public static partial class FdwReferenceAuthAnswerLog
{

    /// <summary>Logs that User was not found, or is not visible to the caller.</summary>
    [MessageLogging(EventId = 30001, Level = LogLevel.Warning, Message = "User was not found, or is not visible to the caller", ClientMessage = "User was not found")]
    public static partial IGenericMessage GetMeUserNotFound(ILogger logger);

    /// <summary>Logs that The assignments read answered a success and no value.</summary>
    [MessageLogging(EventId = 90001, Level = LogLevel.Error, Message = "The assignments read answered a success and no value", ClientMessage = "The assignments could not be read")]
    public static partial IGenericMessage GetMeAssignmentsReturnedNothing(ILogger logger);

    /// <summary>Logs that The role read answered a success and no value.</summary>
    [MessageLogging(EventId = 90002, Level = LogLevel.Error, Message = "The role read answered a success and no value", ClientMessage = "The role could not be read")]
    public static partial IGenericMessage GetMeRoleReturnedNothing(ILogger logger);

    /// <summary>Logs that The memberships read answered a success and no value.</summary>
    [MessageLogging(EventId = 90003, Level = LogLevel.Error, Message = "The memberships read answered a success and no value", ClientMessage = "The memberships could not be read")]
    public static partial IGenericMessage GetMeMembershipsReturnedNothing(ILogger logger);

    /// <summary>Logs that The memberships read answered a success and no value.</summary>
    [MessageLogging(EventId = 90004, Level = LogLevel.Error, Message = "The memberships read answered a success and no value", ClientMessage = "The memberships could not be read")]
    public static partial IGenericMessage GetMyMembershipsMembershipsReturnedNothing(ILogger logger);

    /// <summary>Logs that The tenants read answered a success and no value.</summary>
    [MessageLogging(EventId = 90005, Level = LogLevel.Error, Message = "The tenants read answered a success and no value", ClientMessage = "The tenants could not be read")]
    public static partial IGenericMessage GetMyMembershipsTenantsReturnedNothing(ILogger logger);

    /// <summary>Logs that The request to get me was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50001, Level = LogLevel.Warning, Message = "The request to get me was refused: the caller is not signed in", ClientMessage = "You must be signed in to get me")]
    public static partial IGenericMessage GetMeNotSignedIn(ILogger logger);

    /// <summary>Logs that The request to get my memberships was refused: the caller is not signed in.</summary>
    [MessageLogging(EventId = 50002, Level = LogLevel.Warning, Message = "The request to get my memberships was refused: the caller is not signed in", ClientMessage = "You must be signed in to get my memberships")]
    public static partial IGenericMessage GetMyMembershipsNotSignedIn(ILogger logger);

    /// <summary>Logs that User 'identifier' to update was not found.</summary>
    [MessageLogging(EventId = 30002, Level = LogLevel.Warning, Message = "User '{identifier}' to update was not found", ClientMessage = "User '{identifier}' was not found")]
    public static partial IGenericMessage UpdateUserUserNotFound(ILogger logger, string identifier);

    /// <summary>Logs that The caller's held-membership or role read answered a success and no value.</summary>
    [MessageLogging(EventId = 90006, Level = LogLevel.Error, Message = "The caller's held-membership or role read answered a success and no value", ClientMessage = "Your memberships could not be read")]
    public static partial IGenericMessage GetMyMembershipsHeldOrRolesReturnedNothing(ILogger logger);

    /// <summary>Logs that The held-membership or role read of a user answered a success and no value.</summary>
    [MessageLogging(EventId = 90007, Level = LogLevel.Error, Message = "The held-membership or role read of a user answered a success and no value", ClientMessage = "The user's memberships could not be read")]
    public static partial IGenericMessage GetUserMembershipsHeldOrRolesReturnedNothing(ILogger logger);
}
