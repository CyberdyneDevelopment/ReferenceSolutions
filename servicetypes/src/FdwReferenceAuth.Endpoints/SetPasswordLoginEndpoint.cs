using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Give an account its first password.
/// </summary>
[ExcludeFromCodeCoverage]
public class SetPasswordLoginEndpoint : SetPasswordLoginEndpointBase
{
    /// <inheritdoc />
    public SetPasswordLoginEndpoint(
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService,
        ILogger<SetPasswordLoginEndpoint> logger)
        : base(logger, userProvider, credentialService)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Give an account its first password";
            s.Description = "Sets an initial password, marked must-change.";
        });
        Tags("Users");
    }
}
