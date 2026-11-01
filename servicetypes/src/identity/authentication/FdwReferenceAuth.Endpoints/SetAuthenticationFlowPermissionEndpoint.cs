using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Limit a sign-in flow to holders of a permission (FDW-912).
/// </summary>
[ExcludeFromCodeCoverage]
public class SetAuthenticationFlowPermissionEndpoint : SetAuthenticationFlowPermissionEndpointBase
{
    /// <inheritdoc />
    public SetAuthenticationFlowPermissionEndpoint(IAuthenticationFlowAdministration administration, ILogger<SetAuthenticationFlowPermissionEndpoint> logger)
        : base(logger, administration)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Authentication");
    }
}
