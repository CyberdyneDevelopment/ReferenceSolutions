using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.RestEndpoints.Security;

namespace Reference.Api.Endpoints;

/// <summary>
/// Response model for the public data endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PublicDataResponse
{
    /// <summary>
    /// Gets or sets the message.
    /// </summary>
    public string Message { get; set; } = "";

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
/// A public endpoint demonstrating Standard rate limiting (100 req/min).
/// This endpoint allows anonymous access but applies rate limiting.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class PublicDataEndpoint : PublicEndpointBase<PublicDataResponse>
{
    /// <inheritdoc />
    protected override string Route => "/public/data";

    /// <inheritdoc />
    protected override string EndpointSummary => "Get public data with standard rate limiting";

    /// <inheritdoc />
    protected override string EndpointDescription => "A public endpoint demonstrating Standard rate limiting (100 req/min)";

    /// <inheritdoc />
    protected override string? EndpointTag => "Security Examples";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Response<PublicDataResponse>(200, "Success");
            s.Response(429, "Rate limit exceeded");
        Tags("Security");
        });
    }

    /// <inheritdoc />
#pragma warning disable AsyncFixer01 // Unnecessary async/await - required for FastEndpoints error handling
    public override async Task HandleAsync(CancellationToken ct)
#pragma warning restore AsyncFixer01
    {
        await Send.OkAsync(new PublicDataResponse
        {
            Message = "This is public data with Standard rate limiting (100 req/min)",
            Timestamp = DateTime.UtcNow,
            RateLimitPolicy = "Standard"
        }, ct);
    }
}
