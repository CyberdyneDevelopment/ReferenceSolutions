using System.Diagnostics.CodeAnalysis;
using Fdw.Services.SessionState;
using Fdw.Web.Endpoints.SessionState;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.SessionState;

/// <summary>
/// Deletes a single session state entry for the authenticated user.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteSessionStateEndpoint : DeleteSessionStateEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSessionStateEndpoint"/> class.
    /// </summary>
    public DeleteSessionStateEndpoint(
        ISessionStateService sessionState,
        ILogger<DeleteSessionStateEndpoint>? logger = null)
        : base(sessionState, logger!)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("SessionState");
    }
}
