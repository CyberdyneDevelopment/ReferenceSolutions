using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace FdwReferenceEndpoints;

/// <summary>The coded messages the fdwreferenceendpoints endpoints answer a refusal or a fault with: one per reason, each with the client text it shows (FDW-2335).</summary>
[MessageLoggingTypeCode("RSTENDPTANSWERS")]
public static partial class FdwReferenceEndpointsAnswerLog
{
}
