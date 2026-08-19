using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ReferenceEndpoints.Configuration;

/// <summary>
/// Configuration options for CORS policy.
/// Loaded from appsettings.json "Cors" section.
/// </summary>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public sealed class CorsOptions
{
    /// <summary>Name of the appsettings section these options bind from.</summary>
    public const string SectionName = "Cors";

    /// <summary>Whether the CORS policy is applied at all.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Origins permitted to call this API.</summary>
    public IList<string> Origins { get; set; } = [];

    /// <summary>HTTP methods the policy permits.</summary>
    public IList<string> Methods { get; set; } =
    [
        "GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS"
    ];

    /// <summary>Request headers a caller is allowed to send.</summary>
    public IList<string> Headers { get; set; } =
    [
        "Content-Type", "Authorization", "X-Requested-With", "X-Tenant-Id", "X-Correlation-Id"
    ];

    /// <summary>Response headers the browser is allowed to read.</summary>
    public IList<string> ExposedHeaders { get; set; } =
    [
        "X-Correlation-Id", "X-Request-Id", "WWW-Authenticate",
        "X-RateLimit-Limit", "X-RateLimit-Remaining", "X-RateLimit-Reset"
    ];

    /// <summary>Whether cookies and authorization headers may accompany a cross-origin request.</summary>
    public bool AllowCredentials { get; set; } = true;

    /// <summary>How long a browser may cache the preflight result, in seconds.</summary>
    public int PreflightMaxAgeSeconds { get; set; } = 600;
}
