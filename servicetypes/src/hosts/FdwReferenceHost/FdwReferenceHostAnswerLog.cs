using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceHost.Middleware;

/// <summary>The coded messages the fdwreferencehost endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("RSTHOSTANSWERS")]
public static partial class FdwReferenceHostAnswerLog
{
}
