using System.Diagnostics.CodeAnalysis;
using Fdw.Services.SessionState;
using Fdw.Web.Endpoints.SessionState;
using Microsoft.Extensions.Logging;

namespace ReferenceSessionState.Endpoints;

/// <summary>
/// Upserts a session state value for the authenticated user.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpsertSessionStateEndpoint : UpsertSessionStateEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpsertSessionStateEndpoint"/> class.
    /// </summary>
    public UpsertSessionStateEndpoint(
        ISessionStateService sessionState,
        ILogger<UpsertSessionStateEndpoint>? logger = null)
        : base(sessionState, logger!)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("SessionState");
    }
}
