using System.Diagnostics.CodeAnalysis;
using Fdw.Services.SessionState;
using Fdw.Web.Endpoints.SessionState;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.SessionState;

/// <summary>
/// Gets a session state value by key for the authenticated user.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetSessionStateEndpoint : GetSessionStateEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSessionStateEndpoint"/> class.
    /// </summary>
    public GetSessionStateEndpoint(
        ISessionStateService sessionState,
        ILogger<GetSessionStateEndpoint>? logger = null)
        : base(sessionState, logger!)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("SessionState");
    }
}
