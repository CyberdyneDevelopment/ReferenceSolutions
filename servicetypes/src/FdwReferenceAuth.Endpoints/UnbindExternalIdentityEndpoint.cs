using System.Diagnostics.CodeAnalysis;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Remove an external-identity binding.
/// </summary>
[ExcludeFromCodeCoverage]
public class UnbindExternalIdentityEndpoint : UnbindExternalIdentityEndpointBase
{
    /// <inheritdoc />
    public UnbindExternalIdentityEndpoint(
        UserConfigurationProvider userProvider,
        IExternalIdentityConfigurationProvider externalIdentities,
        ILogger<UnbindExternalIdentityEndpoint> logger)
        : base(logger, userProvider, externalIdentities)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Remove an external-identity binding";
            s.Description = "Unbinds a provider's subject from this account.";
        });
        Tags("Users");
    }
}
