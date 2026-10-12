using System;
using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceHost.Models;

/// <summary>
/// Standard error response for 500 Internal Server Error responses.
/// Includes the reference id for support tracking and contact information.
/// </summary>
// Why: pure DTO serialized directly to the HTTP response body, no logic.
[ExcludeFromCodeCoverage]
public sealed class ErrorResponse
{
    /// <summary>The request's reference id (its TraceIdentifier, logged as RequestId on every line of the request), quoted back so support can find them.</summary>
    public string ReferenceId { get; set; } = "";

    /// <summary>When the failure was produced.</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>HTTP status code carried in the body as well as on the response.</summary>
    public int StatusCode { get; set; } = 500;

    /// <summary>Caller-safe description of the failure.</summary>
    public string Message { get; set; } = "An unexpected error occurred.";

    /// <summary>How to escalate, filled in from the configured support contact.</summary>
    public SupportContactInfo Support { get; set; } = new();
}
