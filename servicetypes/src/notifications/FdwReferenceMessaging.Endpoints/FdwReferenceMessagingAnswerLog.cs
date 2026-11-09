using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceNotifications.Endpoints;

/// <summary>The coded messages the fdwreferencemessaging endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("RSTMSGANSWERS")]
public static partial class FdwReferenceMessagingAnswerLog
{
}
