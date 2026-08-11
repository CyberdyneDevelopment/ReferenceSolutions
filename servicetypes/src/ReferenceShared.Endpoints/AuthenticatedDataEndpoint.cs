using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.RestEndpoints.Security;

namespace ReferenceShared.Endpoints;

/// <summary>
/// Response model for the authenticated data endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AuthenticatedDataResponse
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
    /// Gets or sets the timestamp.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the rate limit policy name.
    /// </summary>
    public string RateLimitPolicy { get; set; } = "";
}

/// <summary>
/// An authenticated endpoint demonstrating higher rate limits for authenticated users.
/// Uses the "Authenticated" rate limit policy (500 req/min).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class AuthenticatedDataEndpoint : AuthenticatedEndpointBase<AuthenticatedDataResponse>
{
    /// <inheritdoc />
    protected override string Route => "/authenticated/data";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get authenticated data with higher rate limit";

    /// <inheritdoc />
    protected override string EndpointDescription => "An authenticated endpoint demonstrating higher rate limits for authenticated users (500 req/min)";

    /// <inheritdoc />
    protected override string? EndpointTag => "Security Examples";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Response<AuthenticatedDataResponse>(200, "Success");
            s.Response(401, "Unauthorized");
            s.Response(429, "Rate limit exceeded");
        Tags("Security");
        });
    }

    /// <inheritdoc />
    public override Task HandleAsync(CancellationToken ct)
    {
        return Send.OkAsync(new AuthenticatedDataResponse
        {
            Message = "This is authenticated data with higher rate limit (500 req/min)",
            User = User.Identity?.Name ?? "Unknown",
            Timestamp = DateTime.UtcNow,
            RateLimitPolicy = "Authenticated"
        }, ct);
    }
}
