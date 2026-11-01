using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint for a user's home-tenant administrator (or the platform administrator) to require them
/// to change their password at next login.
/// </summary>
public class RequirePasswordChangeEndpoint : RequirePasswordChangeEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePasswordChangeEndpoint"/> class.
    /// </summary>
    public RequirePasswordChangeEndpoint(
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService,
        ILogger<RequirePasswordChangeEndpoint> logger)
        : base(userProvider, credentialService, logger)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Users");
    }
}
