using System.Diagnostics.CodeAnalysis;

namespace ReferenceEndpoints.Models;

/// <summary>
/// Contact information for support escalation.
/// </summary>
// Why: pure DTO, only auto-properties, no logic.
[ExcludeFromCodeCoverage]
public sealed class SupportContactInfo
{
    /// <summary>Support email address given to the caller.</summary>
    public string Email { get; set; } = "";

    /// <summary>Support phone number, when one is published.</summary>
    public string? Phone { get; set; }

    /// <summary>Support portal address, when one is published.</summary>
    public string? PortalUrl { get; set; }

    /// <summary>What the caller should do next.</summary>
    public string Instructions { get; set; } = "If this error persists, please contact support with the Request ID above.";

    /// <summary>Advertised response time, when one is published.</summary>
    public int? ExpectedResponseTimeHours { get; set; }
}
