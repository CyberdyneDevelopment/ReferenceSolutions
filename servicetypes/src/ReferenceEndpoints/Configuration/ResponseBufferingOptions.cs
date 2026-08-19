using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ReferenceEndpoints.Configuration;

/// <summary>
/// Configuration for the framework response-buffering middleware.
/// </summary>
/// <remarks>
/// <para>
/// When enabled, every HTTP response body is buffered to a MemoryStream and emitted with an
/// explicit Content-Length header rather than chunked Transfer-Encoding. Some HTTP clients
/// (notably Postman/Newman's internal <c>pm.sendRequest</c>) cannot reassemble chunked JSON
/// responses; buffering forces a fixed Content-Length so those clients can parse the body.
/// </para>
/// <para>
/// This is OFF by default. Reference apps that run integration tests with such clients can
/// opt in via <c>"ResponseBuffering": { "Enabled": true }</c> in appsettings.
/// </para>
/// <para>
/// Bound to the <c>ResponseBuffering</c> configuration section by
/// <c>ResponseBufferingMiddleware</c>.
/// </para>
/// </remarks>
// Why: pure DTO, only auto-properties bound from IOptions, no logic.
[ExcludeFromCodeCoverage]
public sealed class ResponseBufferingOptions
{
    /// <summary>
    /// Gets or sets whether response buffering is active. Default <see langword="false"/>.
    /// </summary>
    // Why: opt-in so production deployments don't pay the per-request buffer-then-copy cost
    // unless they need it (typically for integration test or known-bad-client workloads).
    public bool Enabled { get; set; }

    /// <summary>
    /// Gets or sets the maximum buffered body size in bytes. Responses larger than this stream
    /// straight through to the client without buffering. Default 4 MiB.
    /// </summary>
    // Why: cap memory so a malicious or pathological client can't force unbounded buffering.
    public int MaxBufferBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>
    /// Gets or sets exact request path prefixes that bypass buffering. Use for streaming
    /// endpoints (SignalR hubs, SSE, large downloads, etc.).
    /// </summary>
    public IList<string> ExcludePathPrefixes { get; set; } = new List<string>
    {
        "/hubs/",
        "/health",
    };
}
