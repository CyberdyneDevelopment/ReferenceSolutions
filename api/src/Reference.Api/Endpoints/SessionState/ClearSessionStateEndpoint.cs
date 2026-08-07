using System.Diagnostics.CodeAnalysis;
using Fdw.Services.SessionState;
using Fdw.Web.Endpoints.SessionState;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.SessionState;

/// <summary>
/// Clears all session state entries for the authenticated user.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ClearSessionStateEndpoint : ClearSessionStateEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClearSessionStateEndpoint"/> class.
    /// </summary>
    public ClearSessionStateEndpoint(
        ISessionStateService sessionState,
        ILogger<ClearSessionStateEndpoint>? logger = null)
        : base(sessionState, logger!)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("SessionState");
    }
}
