using System.Diagnostics.CodeAnalysis;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Bind an external identity to an account.
/// </summary>
[ExcludeFromCodeCoverage]
public class BindExternalIdentityEndpoint : BindExternalIdentityEndpointBase
{
    /// <inheritdoc />
    public BindExternalIdentityEndpoint(
        UserConfigurationProvider userProvider,
        IExternalIdentityConfigurationProvider externalIdentities,
        ILogger<BindExternalIdentityEndpoint> logger)
        : base(logger, userProvider, externalIdentities)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Bind an external identity to an account";
            s.Description = "Binds a provider's subject so its first sign-in resolves here instead of provisioning a second account.";
        });
        Tags("Users");
    }
}
