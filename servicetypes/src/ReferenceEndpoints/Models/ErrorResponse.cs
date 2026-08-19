using System;
using System.Diagnostics.CodeAnalysis;

namespace ReferenceEndpoints.Models;

/// <summary>
/// Standard error response for 500 Internal Server Error responses.
/// Includes request ID for support tracking and contact information.
/// </summary>
// Why: pure DTO serialized directly to the HTTP response body, no logic.
[ExcludeFromCodeCoverage]
public sealed class ErrorResponse
{
    /// <summary>Correlation id for this request, quoted back so support can find it in the logs.</summary>
    public string RequestId { get; set; } = "";

    /// <summary>When the failure was produced.</summary>
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>HTTP status code carried in the body as well as on the response.</summary>
    public int StatusCode { get; set; } = 500;

    /// <summary>Caller-safe description of the failure.</summary>
    public string Message { get; set; } = "An unexpected error occurred.";

    /// <summary>How to escalate, filled in from the configured support contact.</summary>
    public SupportContactInfo Support { get; set; } = new();
}
