using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Open a sign-in flow to every active user (FDW-912).
/// </summary>
[ExcludeFromCodeCoverage]
public class RemoveAuthenticationFlowPermissionEndpoint : RemoveAuthenticationFlowPermissionEndpointBase
{
    /// <inheritdoc />
    public RemoveAuthenticationFlowPermissionEndpoint(IAuthenticationFlowAdministration administration, ILogger<RemoveAuthenticationFlowPermissionEndpoint> logger)
        : base(logger, administration)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Authentication");
    }
}
