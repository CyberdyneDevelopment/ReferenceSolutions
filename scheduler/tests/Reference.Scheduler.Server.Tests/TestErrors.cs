using Fdw.Messages;

namespace Reference.Scheduler.Server.Tests;

/// <summary>
/// Pre-built error messages for test assertions. Avoids FDW002 violations
/// from using GenericResult.Failure("plain string") in tests.
/// </summary>
internal static class TestErrors
{
    internal static readonly IGenericMessage Failed = new GenericMessage(MessageSeverity.Error, "Failed");
    internal static readonly IGenericMessage InsertFailed = new GenericMessage(MessageSeverity.Error, "Insert failed");
    internal static readonly IGenericMessage DeleteFailed = new GenericMessage(MessageSeverity.Error, "Delete failed");
    internal static readonly IGenericMessage QueryFailed = new GenericMessage(MessageSeverity.Error, "Query failed");
    internal static readonly IGenericMessage UpdateFailed = new GenericMessage(MessageSeverity.Error, "Update failed");
    internal static readonly IGenericMessage DispatchFailed = new GenericMessage(MessageSeverity.Error, "Dispatch failed");
    internal static readonly IGenericMessage NotFound = new GenericMessage(MessageSeverity.Warning, "Not found");
}
