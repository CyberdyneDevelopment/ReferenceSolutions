using System.Diagnostics.CodeAnalysis;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Selects which <c>ForeignToken</c>-first flow to run.
/// </summary>
/// <remarks>
/// The presented token itself is never in this body — <c>HttpForeignTokenAccessor</c> reads it
/// straight off the request (the RFC 8693 <c>subject_token</c> form field first, then a bare
/// <c>Authorization: Bearer</c> header) the same way <c>HttpOidcCallbackAccessor</c> already reads
/// <c>code</c>/<c>state</c> for the redirect flow. Naming the flow is what lets one endpoint serve
/// every provider a deployment configures — two providers live at once as two flows and one step,
/// and the caller's choice of button selects which flow runs.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class TokenExchangeRequest
{
    /// <summary>Gets or sets the flow to run, e.g. <c>"EntraForeignToken"</c> or <c>"Auth0Login"</c>.</summary>
    public required string FlowName { get; set; }
}
