using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>A session's CSRF token (FDW-903).</summary>
[ExcludeFromCodeCoverage]
public sealed class SessionCsrfTokenResponse
{
    /// <summary>
    /// Gets or sets the token. The browser app keeps it in memory and echoes it, in the header its
    /// Session service names, on every write. Fetching again issues a new one and retires this one.
    /// </summary>
    public string Token { get; set; } = string.Empty;
}
