using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace ReferenceUsers.Endpoints;

/// <summary>
/// Endpoint to reset a user's password (admin operation).
/// </summary>
public class ResetPasswordEndpoint : ResetPasswordEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ResetPasswordEndpoint"/> class.
    /// </summary>
    public ResetPasswordEndpoint(
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService,
        ILoggerFactory loggerFactory)
        : base(userProvider, credentialService, loggerFactory)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Users");
    }
}
