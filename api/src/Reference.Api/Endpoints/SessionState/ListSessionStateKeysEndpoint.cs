using System.Diagnostics.CodeAnalysis;
using Fdw.Services.SessionState;
using Fdw.Web.Endpoints.SessionState;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.SessionState;

/// <summary>
/// Lists all session state keys for the authenticated user.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListSessionStateKeysEndpoint : ListSessionStateKeysEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListSessionStateKeysEndpoint"/> class.
    /// </summary>
    public ListSessionStateKeysEndpoint(
        ISessionStateService sessionState,
        ILogger<ListSessionStateKeysEndpoint>? logger = null)
        : base(sessionState, logger!)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("SessionState");
    }
}
