using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Switch a sign-in flow on or off (FDW-912).
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateAuthenticationFlowEndpoint : UpdateAuthenticationFlowEndpointBase
{
    /// <inheritdoc />
    public UpdateAuthenticationFlowEndpoint(IAuthenticationFlowAdministration administration, ILogger<UpdateAuthenticationFlowEndpoint> logger)
        : base(logger, administration)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Authentication");
    }
}
