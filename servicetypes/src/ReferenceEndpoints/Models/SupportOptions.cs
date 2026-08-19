using System.Diagnostics.CodeAnalysis;

namespace ReferenceEndpoints.Models;

/// <summary>
/// Configuration options for support contacts.
/// Bind from appsettings.json "Support" section.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public sealed class SupportOptions
{
    /// <summary>Support email address published in error responses.</summary>
    public string Email { get; set; } = "";

    /// <summary>Support phone number, when one is published.</summary>
    public string? Phone { get; set; }

    /// <summary>Support portal address, when one is published.</summary>
    public string? PortalUrl { get; set; }

    /// <summary>Advertised response time in hours.</summary>
    public int ExpectedResponseTimeHours { get; set; } = 24;

    /// <summary>What a caller should do next, copied into the error response.</summary>
    public string Instructions { get; set; } = "If this error persists, please contact support with the Request ID above.";
}
