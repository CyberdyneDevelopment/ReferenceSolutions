using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Take an account's password login away (FDW-910).
/// </summary>
[ExcludeFromCodeCoverage]
public class RemovePasswordLoginEndpoint : RemovePasswordLoginEndpointBase
{
    /// <inheritdoc />
    public RemovePasswordLoginEndpoint(
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService,
        ILogger<RemovePasswordLoginEndpoint> logger)
        : base(logger, userProvider, credentialService)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Remove an account's password login";
            s.Description = "Disables the account's password. Its external identities and API keys are untouched.";
        });
        Tags("Users");
    }
}
