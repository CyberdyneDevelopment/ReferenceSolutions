using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Results;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using ReferenceAuthentication.OpenIddict.Logging;

namespace ReferenceAuthentication.OpenIddict.Hosting;

/// <summary>
/// Gives a confidential application the client secret it is declared to require.
/// </summary>
/// <remarks>
/// A confidential client authenticates with a secret, and OpenIddict compares an incoming secret
/// against a hash it produced itself — so the value has to be written through
/// <c>IOpenIddictApplicationManager</c>, which hashes on the way in. A row inserted by SQL can
/// declare the client but cannot give it a usable secret, which is why every client seeded that way
/// answers client_credentials with "The credentials provided are invalid".
///
/// The secret is read from a secret manager, never held here, and the key is derived from the client
/// id: <c>fdw.scheduler</c> reads <c>SCHEDULER_CLIENT_SECRET</c>. A client whose secret is not
/// present is left alone rather than given a generated one — an unprovisioned client failing at its
/// first exchange is recoverable; one silently holding a secret nobody else knows is not.
/// </remarks>
public sealed class OpenIddictClientSecretProvisioner
{
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="OpenIddictClientSecretProvisioner"/> class.</summary>
    /// <param name="logger">The logger; may be null when DI has no logging configured.</param>
    public OpenIddictClientSecretProvisioner(ILogger<OpenIddictClientSecretProvisioner>? logger) =>
        _logger = logger ?? NullLogger<OpenIddictClientSecretProvisioner>.Instance;

    /// <summary>
    /// Sets the client secret on every confidential application that has none and whose secret is
    /// resolvable.
    /// </summary>
    /// <param name="services">The built service provider.</param>
    /// <param name="secretManagerName">The secret manager holding the client secrets.</param>
    /// <param name="clientIds">The client ids to provision.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The number of clients provisioned.</returns>
    public async Task<IGenericResult<int>> Provision(
        IServiceProvider services,
        string secretManagerName,
        IReadOnlyList<string> clientIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(clientIds);

        if (string.IsNullOrEmpty(secretManagerName))
        {
            return GenericResult<int>.Failure(
                OpenIddictProviderLog.ClientSecretProvisionNoConfig(_logger));
        }

        var applications = services.GetService<IOpenIddictApplicationManager>();
        if (applications is null)
        {
            return GenericResult<int>.Failure(
                OpenIddictProviderLog.ClientSecretProvisionNoConfig(_logger));
        }

        var managerResult = await services
            .GetRequiredService<IFdwServiceProvider<ISecretManager, SecretManagerConfiguration>>()
            .Get(secretManagerName, cancellationToken).ConfigureAwait(false);
        if (!managerResult.IsSuccess || managerResult.Value is not { } secretManager)
        {
            return GenericResult<int>.Failure(
                OpenIddictProviderLog.ClientSecretProvisionNoConfig(_logger));
        }

        var provisioned = 0;
        for (var i = 0; i < clientIds.Count; i++)
        {
            var clientId = clientIds[i];
            var application = await applications.FindByClientIdAsync(clientId, cancellationToken).ConfigureAwait(false);
            if (application is null)
            {
                OpenIddictProviderLog.ClientSecretNotConfigured(_logger, clientId, SecretKeyFor(clientId));
                continue;
            }

            // Why unconditionally rather than only when absent: OpenIddict does not hand a stored
            // secret back, so there is nothing to compare against. Re-hashing the same secret each
            // start is harmless, and it means a secret rotated in the secret manager takes effect.

            var secretResult = await secretManager
                .Execute(GetSecretManagerCommand.Latest(container: null, secretKey: SecretKeyFor(clientId)), cancellationToken)
                .ConfigureAwait(false);
            if (!secretResult.IsSuccess || secretResult.Value is not SecretValue secret)
            {
                OpenIddictProviderLog.ClientSecretNotConfigured(_logger, clientId, SecretKeyFor(clientId));
                continue;
            }

            // Why the using: SecretValue holds the plaintext and is disposable so it does not outlive
            // the one call that needs it.
            using (secret)
            {
                var descriptor = new OpenIddictApplicationDescriptor();
                await applications.PopulateAsync(descriptor, application, cancellationToken).ConfigureAwait(false);
                descriptor.ClientSecret = secret.GetStringValue();
                await applications.UpdateAsync(application, descriptor, cancellationToken).ConfigureAwait(false);
            }

            OpenIddictProviderLog.ClientSecretProvisioned(_logger, clientId);
            provisioned++;
        }

        return GenericResult<int>.Success(provisioned);
    }

    /// <summary>Derives the secret key a client's secret is stored under.</summary>
    /// <param name="clientId">The OpenIddict client id.</param>
    /// <returns>The secret key name.</returns>
    /// <remarks><c>fdw.scheduler</c> becomes <c>SCHEDULER_CLIENT_SECRET</c>.</remarks>
    private static string SecretKeyFor(string clientId) =>
        clientId.StartsWith("fdw.", StringComparison.Ordinal)
            ? clientId[4..].ToUpperInvariant() + "_CLIENT_SECRET"
            : clientId.ToUpperInvariant() + "_CLIENT_SECRET";
}
