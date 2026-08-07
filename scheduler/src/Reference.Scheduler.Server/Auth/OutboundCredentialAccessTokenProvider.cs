using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Tokens.Outbound;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Fdw.ServiceTypes;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Reference.Scheduler.Server.Configuration;
using Reference.Scheduler.Server.Logging;
using ReferenceAuthentication.OpenIddict.Services;

namespace Reference.Scheduler.Server.Auth;

/// <summary>
/// <see cref="IAccessTokenProvider"/> that bridges the FDW <see cref="IOutboundCredentialService"/>
/// to the <see cref="BearerTokenHandler"/> for the scheduler's outbound pipeline-dispatch HTTP client.
/// Acquires a client-credentials token (client id <c>fdw.scheduler</c>, scope <c>fdw.api</c> — the
/// only registered OpenIddict scope <c>fdw.scheduler</c> is permitted to request) from the API's
/// <c>/connect/token</c> endpoint — the <see cref="OpenIddictOutboundCredentialService"/> resolves
/// that endpoint from the OpenIddict <c>Authority</c> shipped in <c>appsettings.json</c>, which
/// points at the API. The <c>pipelines:execute</c> permission the ETL server checks rides in the
/// token's <c>perm</c> claim (baked from the client's ServicePipelineRunner role) regardless of scope.
/// </summary>
/// <remarks>
/// The client secret is NEVER inlined. It is resolved at runtime through the configured secret
/// manager (<c>EnvSecrets</c> → <c>FDW_SECRET_SCHEDULER_CLIENT_SECRET</c>), exactly the way the
/// OpenIddict signing key is resolved. Resolution is lazy (on first acquisition) so a scoped secret
/// manager can be used without sync-over-async at registration time.
/// </remarks>
public sealed class OutboundCredentialAccessTokenProvider : IAccessTokenProvider
{
    private readonly IOutboundCredentialService _outboundCredentialService;
    private readonly IFdwServiceProvider<ISecretManager, SecretManagerConfiguration> _secretManagerProvider;
    private readonly OutboundClientCredentialsConfiguration _config;
    private readonly ILogger<OutboundCredentialAccessTokenProvider> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutboundCredentialAccessTokenProvider"/> class.
    /// </summary>
    /// <param name="outboundCredentialService">The FDW outbound credential service (client-credentials flow).</param>
    /// <param name="secretManagerProvider">Provider used to resolve the client secret at runtime.</param>
    /// <param name="config">The outbound client-credentials configuration (client id + secret references + scopes).</param>
    /// <param name="logger">Logger instance.</param>
    public OutboundCredentialAccessTokenProvider(
        IOutboundCredentialService outboundCredentialService,
        IFdwServiceProvider<ISecretManager, SecretManagerConfiguration> secretManagerProvider,
        IOptions<OutboundClientCredentialsConfiguration> config,
        ILogger<OutboundCredentialAccessTokenProvider>? logger)
    {
        ArgumentNullException.ThrowIfNull(outboundCredentialService);
        ArgumentNullException.ThrowIfNull(secretManagerProvider);
        ArgumentNullException.ThrowIfNull(config);
        _outboundCredentialService = outboundCredentialService;
        _secretManagerProvider = secretManagerProvider;
        _config = config.Value;
        _logger = logger ?? NullLogger<OutboundCredentialAccessTokenProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default)
    {
        // Why: fail loud if the outbound identity isn't configured — never fall back to an anonymous
        // dispatch. Returning null makes BearerTokenHandler omit the header and the ETL server returns 401.
        if (string.IsNullOrEmpty(_config.ClientId)
            || string.IsNullOrEmpty(_config.SecretManagerName)
            || string.IsNullOrEmpty(_config.SecretKeyName))
        {
            OutboundTokenLog.ClientCredentialsConfigurationMissing(_logger);
            return null;
        }

        OutboundTokenLog.TokenAcquisitionStarted(_logger, _config.ClientId, string.Join(" ", _config.Scopes));

        try
        {
            var clientSecret = await ResolveClientSecret(cancellationToken).ConfigureAwait(false);
            if (clientSecret is null)
            {
                return null;
            }

            var result = await _outboundCredentialService.Acquire(
                new OutboundCredentialRequest
                {
                    ClientId = _config.ClientId,
                    ClientSecret = clientSecret,
                    Scopes = _config.Scopes,
                },
                cancellationToken).ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                OutboundTokenLog.TokenAcquisitionFailed(
                    _logger,
                    _config.ClientId,
                    string.IsNullOrEmpty(result.CurrentMessage) ? "no message" : result.CurrentMessage);
                return null;
            }

            OutboundTokenLog.TokenAcquired(_logger, _config.ClientId);
            return result.Value.AccessToken;
        }
        catch (Exception ex)
        {
            OutboundTokenLog.TokenAcquisitionException(_logger, ex, _config.ClientId);
            return null;
        }
    }

    // Why: resolve the client secret the same way OpenIddictSigningKeyLoader resolves the signing key
    // — look up the named secret manager, run a GetSecretManagerCommand for the named key. The value
    // is never inlined and is read fresh per acquisition (the outbound service caches the resulting
    // token, so this only runs when a new token must be minted).
    private async Task<string?> ResolveClientSecret(CancellationToken cancellationToken)
    {
        var managerResult = await _secretManagerProvider
            .Get(_config.SecretManagerName, cancellationToken)
            .ConfigureAwait(false);
        if (!managerResult.IsSuccess || managerResult.Value is null)
        {
            OutboundTokenLog.SecretManagerNotFound(_logger, _config.SecretManagerName, _config.SecretKeyName);
            return null;
        }

        var secretResult = await managerResult.Value
            .Execute(GetSecretManagerCommand.Latest(container: null, secretKey: _config.SecretKeyName), cancellationToken)
            .ConfigureAwait(false);
        if (!secretResult.IsSuccess)
        {
            var reason = secretResult.CurrentMessage;
            OutboundTokenLog.SecretReadFailed(
                _logger,
                _config.SecretManagerName,
                _config.SecretKeyName,
                string.IsNullOrEmpty(reason) ? "secret manager returned a failure with no message" : reason);
            return null;
        }

        if (secretResult.Value is not SecretValue secret)
        {
            OutboundTokenLog.SecretMissing(_logger, _config.SecretManagerName, _config.SecretKeyName);
            return null;
        }

        using (secret)
        {
            var value = secret.GetStringValue();
            if (string.IsNullOrEmpty(value))
            {
                OutboundTokenLog.SecretMissing(_logger, _config.SecretManagerName, _config.SecretKeyName);
                return null;
            }

            return value;
        }
    }
}
