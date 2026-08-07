using System.Diagnostics.CodeAnalysis;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Logging;

/// <summary>
/// MessageLogging definitions for DDL operations.
/// EventId range: 8550-8599
/// </summary>
[ExcludeFromCodeCoverage]
public static partial class DdlLog
{
    [MessageLogging(EventId = 8550, Level = LogLevel.Information, Message = "Generating DDL for connection '{connectionName}'")]
    public static partial IGenericMessage GeneratingDdl(ILogger logger, string connectionName);

    [MessageLogging(EventId = 8551, Level = LogLevel.Information, Message = "DDL generated for connection '{connectionName}'")]
    public static partial IGenericMessage DdlGenerated(ILogger logger, string connectionName);

    [MessageLogging(EventId = 8552, Level = LogLevel.Error, Message = "DDL generation failed for connection '{connectionName}': {message}")]
    public static partial IGenericMessage DdlGenerationFailed(ILogger logger, string connectionName, string message);

    [MessageLogging(EventId = 8553, Level = LogLevel.Information, Message = "Executing DDL on connection '{connectionName}'")]
    public static partial IGenericMessage ExecutingDdl(ILogger logger, string connectionName);

    [MessageLogging(EventId = 8554, Level = LogLevel.Information, Message = "DDL executed on connection '{connectionName}'")]
    public static partial IGenericMessage DdlExecuted(ILogger logger, string connectionName);

    [MessageLogging(EventId = 8555, Level = LogLevel.Error, Message = "DDL execution failed on connection '{connectionName}': {message}")]
    public static partial IGenericMessage DdlExecutionFailed(ILogger logger, string connectionName, string message);
}
