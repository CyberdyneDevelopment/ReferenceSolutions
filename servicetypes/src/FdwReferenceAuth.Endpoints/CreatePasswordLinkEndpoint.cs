using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to create a one-time set-password link for a user (admin operation, FDW-941).
/// </summary>
public class CreatePasswordLinkEndpoint : CreatePasswordLinkEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePasswordLinkEndpoint"/> class.
    /// </summary>
    public CreatePasswordLinkEndpoint(
        UserConfigurationProvider userProvider,
        IPasswordLinkService linkService,
        ILogger<CreatePasswordLinkEndpoint> logger)
        : base(userProvider, linkService, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Users");
    }
}
