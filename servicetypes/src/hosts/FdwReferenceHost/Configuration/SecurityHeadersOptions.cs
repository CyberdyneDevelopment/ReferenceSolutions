using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceHost.Configuration;

/// <summary>
/// Configuration options for security headers middleware.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public sealed class SecurityHeadersOptions
{
    /// <summary>Whether the response may be rendered inside a frame.</summary>
    public bool AllowFraming { get; set; }

    /// <summary>Explicit policy to send, overriding the default one.</summary>
    public string? ContentSecurityPolicy { get; set; }

    /// <summary>Whether to send the built-in policy when none is configured.</summary>
    public required bool EnableDefaultCsp { get; set; }

    /// <summary>
    /// Paths that should have no-cache headers. Defaults to auth and user paths.
    /// </summary>
    public required string[] SensitivePaths { get; set; }
}
