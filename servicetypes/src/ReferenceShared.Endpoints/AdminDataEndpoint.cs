using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.RestEndpoints.Security;

namespace ReferenceShared.Endpoints;

/// <summary>
/// Response model for the admin data endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AdminDataResponse
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

    /// <summary>
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the rate limit policy name.
    /// </summary>
    public string RateLimitPolicy { get; set; } = "";
}

/// <summary>
/// An admin endpoint requiring the Admin role and using the highest rate limit policy.
/// Uses the "Admin" rate limit policy (10000 req/min).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AdminDataEndpoint : AdminEndpointBase<AdminDataResponse>
{
    /// <inheritdoc />
    protected override string Route => "/admin/data";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get admin data with highest rate limit";

    /// <inheritdoc />
    protected override string EndpointDescription => "An admin endpoint requiring the Admin role with highest rate limit policy (10000 req/min)";

    /// <inheritdoc />
    protected override string? EndpointTag => "Security Examples";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Response<AdminDataResponse>(200, "Success");
            s.Response(401, "Unauthorized");
            s.Response(403, "Forbidden - Admin role required");
            s.Response(429, "Rate limit exceeded");
        Tags("Security");
        });
    }

    /// <inheritdoc />
    public override Task HandleAsync(CancellationToken ct)
    {
        var roles = User.Claims
            .Where(c => string.Equals(c.Type, "role", StringComparison.Ordinal) || string.Equals(c.Type, ClaimTypes.Role, StringComparison.Ordinal))
            .Select(c => c.Value)
            .ToArray();

        return Send.OkAsync(new AdminDataResponse
        {
            Message = "This is admin data with Admin role and highest rate limit (10000 req/min)",
            User = User.Identity?.Name ?? "Unknown",
            Roles = roles,
            Timestamp = DateTime.UtcNow,
            RateLimitPolicy = "Admin"
        }, ct);
    }
}
