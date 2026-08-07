using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Tokens.Outbound;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ReferenceAuthentication.TokenProviders;

/// <summary>
/// <see cref="IAccessTokenProvider"/> that bridges the FDW
/// <see cref="IOutboundCredentialService"/> to the <see cref="BearerTokenHandler"/> for
/// inter-service HTTP clients. Acquires a client-credentials token for the configured client identity.
/// </summary>
public sealed class OutboundCredentialAccessTokenProvider : IAccessTokenProvider
{
    private readonly IOutboundCredentialService _outboundCredentialService;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly ILogger<OutboundCredentialAccessTokenProvider> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="OutboundCredentialAccessTokenProvider"/>.
    /// </summary>
    /// <param name="outboundCredentialService">The FDW outbound credential service (client-credentials flow).</param>
    /// <param name="clientId">The OAuth 2.0 client identifier.</param>
    /// <param name="clientSecret">The client secret. Resolved from environment at construction time.</param>
    /// <param name="logger">Logger instance.</param>
    public OutboundCredentialAccessTokenProvider(
        IOutboundCredentialService outboundCredentialService,
        string clientId,
        string clientSecret,
        ILogger<OutboundCredentialAccessTokenProvider>? logger)
    {
        _outboundCredentialService = outboundCredentialService ?? throw new ArgumentNullException(nameof(outboundCredentialService));
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        ArgumentException.ThrowIfNullOrEmpty(clientSecret);
        _clientId = clientId;
        _clientSecret = clientSecret;
        _logger = logger ?? NullLogger<OutboundCredentialAccessTokenProvider>.Instance;
    }

    /// <inheritdoc />
    public async Task<string?> GetAccessToken(CancellationToken cancellationToken = default)
    {
        var request = new OutboundCredentialRequest
        {
            ClientId = _clientId,
            ClientSecret = _clientSecret,
        };

        var result = await _outboundCredentialService.Acquire(request, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
        {
            _logger.LogWarning(
                "Failed to acquire outbound token for client '{ClientId}': {Message}",
                _clientId,
                result.CurrentMessage);
            return null;
        }

        return result.Value?.AccessToken;
    }
}
