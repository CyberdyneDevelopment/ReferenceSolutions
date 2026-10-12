using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// List every configured sign-in flow and who may use it (FDW-912).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListAuthenticationFlowsEndpoint : ListAuthenticationFlowsEndpointBase
{
    /// <inheritdoc />
    public ListAuthenticationFlowsEndpoint(IAuthenticationFlowAdministration administration, ILogger<ListAuthenticationFlowsEndpoint> logger)
        : base(logger, administration)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Authentication");
    }
}
