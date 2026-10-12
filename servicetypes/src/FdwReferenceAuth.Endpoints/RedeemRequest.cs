using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>The one-time code the sign-in callback returned the browser with.</summary>
[ExcludeFromCodeCoverage]
public sealed class RedeemRequest
{
    /// <summary>Gets or sets the <c>auth_code</c> query value the callback appended to the return URL.</summary>
    public required string AuthCode { get; set; }
}
