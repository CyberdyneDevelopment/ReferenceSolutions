using ReferenceAuth.Endpoints.Logging;
using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.RestEndpoints.Security;
using Microsoft.Extensions.Logging;
using ReferenceShared.Endpoints.Logging;

namespace ReferenceShared.Endpoints;

/// <summary>
/// Response model for the protected endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ProtectedResponse
{
    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Gets or sets the authenticated user name.
    /// </summary>
    public string User { get; set; } = "";

    /// <summary>
    /// Gets or sets the user's roles.
    /// </summary>
    public string[] Roles { get; set; } = [];
}

/// <summary>
/// A protected endpoint that requires JWT authentication.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ProtectedEndpoint : AuthenticatedEndpointBase<ProtectedResponse>
{
    private readonly ILogger<ProtectedEndpoint> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProtectedEndpoint"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    public ProtectedEndpoint(ILogger<ProtectedEndpoint> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    protected override string Route => "/protected";

    /// <inheritdoc />
    protected override string? RateLimitPolicy => null;

    /// <inheritdoc />
    protected override string EndpointSummary => "Access protected resource";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns information about the authenticated user. Requires valid JWT authentication.";

    /// <inheritdoc />
#pragma warning disable AsyncFixer01 // Unnecessary async/await - required for FastEndpoints error handling
    public override async Task HandleAsync(CancellationToken ct)
#pragma warning restore AsyncFixer01
    {
        var username = User.Identity?.Name ?? "Unknown";
        var roles = User.Claims
            .Where(c => string.Equals(c.Type, "role", StringComparison.Ordinal) || string.Equals(c.Type, ClaimTypes.Role, StringComparison.Ordinal))
            .Select(c => c.Value)
            .ToArray();

        AuthenticationLog.ProtectedResourceAccessed(_logger, username);

        await Send.OkAsync(new ProtectedResponse
        {
            Message = "You have accessed a protected resource!",
            User = username,
            Roles = roles
        }, ct);
    }
}
