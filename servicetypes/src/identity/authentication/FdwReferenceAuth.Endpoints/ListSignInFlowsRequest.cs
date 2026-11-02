using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>Request for <c>GET /auth/sign-in-flows?returnUrl=</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed class ListSignInFlowsRequest
{
    /// <summary>Gets or sets the absolute URL of the page that will offer the flows (query).</summary>
    public string ReturnUrl { get; set; } = string.Empty;
}
