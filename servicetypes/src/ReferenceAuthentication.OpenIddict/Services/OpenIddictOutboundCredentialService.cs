using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using Fdw.Services.Authentication.Abstractions.Tokens.Outbound;
using ReferenceAuthentication.OpenIddict.Logging;
using Fdw.Services.TokenManagers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Services;

/// <summary>
/// <see cref="IOutboundCredentialService"/> that acquires client-credentials tokens
/// from this deployment's own OpenIddict <c>/connect/token</c> endpoint.
/// Tokens are cached until within 60 seconds of expiry.
/// </summary>
internal sealed class OpenIddictOutboundCredentialService : IOutboundCredentialService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OpenIddictOutboundCredentialService> _logger;

    // Why: Simple in-memory cache keyed by "clientId|scope1 scope2". No distributed cache needed
    // for outbound tokens — each process maintains its own credential set.
    private readonly Dictionary<string, (OutboundCredential Credential, DateTimeOffset ExpiresAt)> _cache
        = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private static readonly TimeSpan RefreshThreshold = TimeSpan.FromSeconds(60);

    public OpenIddictOutboundCredentialService(
        IHttpClientFactory httpClientFactory,
        IServiceScopeFactory scopeFactory,
        ILogger<OpenIddictOutboundCredentialService>? logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(scopeFactory);
        _httpClientFactory = httpClientFactory;
        _scopeFactory = scopeFactory;
        _logger = logger ?? NullLogger<OpenIddictOutboundCredentialService>.Instance;
    }

    /// <inheritdoc />
    public async Task<IGenericResult<OutboundCredential>> Acquire(
        OutboundCredentialRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OpenIddictProviderLog.OutboundAcquireStarted(_logger, request.ClientId);

        var cacheKey = BuildCacheKey(request);

        await _cacheLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_cache.TryGetValue(cacheKey, out var cached)
                && cached.ExpiresAt.Subtract(RefreshThreshold) > DateTimeOffset.UtcNow)
            {
                OpenIddictProviderLog.OutboundAcquiredFromCache(_logger, request.ClientId);
                return GenericResult<OutboundCredential>.Success(cached.Credential);
            }

            var freshResult = await FetchFromEndpoint(request, cancellationToken).ConfigureAwait(false);
            if (!freshResult.IsSuccess)
                return freshResult;

            _cache[cacheKey] = (freshResult.Value!, freshResult.Value!.ExpiresAt);
            return freshResult;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    /// <inheritdoc />
    public void Dispose() { }

    private async Task<IGenericResult<OutboundCredential>> FetchFromEndpoint(
        OutboundCredentialRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var form = new FormUrlEncodedContent(new[]
            {
                KeyValuePair.Create("grant_type", "client_credentials"),
                KeyValuePair.Create("client_id", request.ClientId),
                KeyValuePair.Create("client_secret", request.ClientSecret),
                KeyValuePair.Create("scope", string.Join(" ", request.Scopes)),
            });

            var endpointResult = await ResolveTokenEndpoint(request, cancellationToken).ConfigureAwait(false);
            if (!endpointResult.IsSuccess)
                return endpointResult.ToNewResult<OutboundCredential>();

            using var client = _httpClientFactory.CreateClient("OpenIddictOutbound");
            using var response = await client.PostAsync(endpointResult.Value, form, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return GenericResult<OutboundCredential>.Failure(
                    OpenIddictProviderLog.OutboundAcquireFailed(_logger, request.ClientId,
                        $"HTTP {(int)response.StatusCode}: {body}"));
            }

            using var doc = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                cancellationToken: cancellationToken).ConfigureAwait(false);

            var root = doc.RootElement;
            var accessToken = root.GetProperty("access_token").GetString()!;
            var expiresIn = root.TryGetProperty("expires_in", out var expIn) ? expIn.GetInt32() : 3600;
            // Why: RFC 6749 specifies token_type MUST be "Bearer" if absent; the ternary avoids a ?? string literal.
            var tokenType = root.TryGetProperty("token_type", out var tt) && tt.GetString() is { Length: > 0 } ttStr
                ? ttStr
                : "Bearer";

            var credential = new OutboundCredential
            {
                AccessToken = accessToken,
                TokenType = tokenType,
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn),
            };

            return GenericResult<OutboundCredential>.Success(credential);
        }
        // Why: HttpRequestException (network/transport failure) and JsonException (malformed token
        // response body) are the documented failure modes of this HTTP round-trip — caught
        // specifically so the logged reason names the actual cause. The result carries the FULL
        // exception chain via FlattenException so a caller never sees just "One or more errors occurred."
        catch (HttpRequestException ex)
        {
            OpenIddictProviderLog.OutboundAcquireFailed(_logger, request.ClientId, ex.Message);
            return GenericResult<OutboundCredential>.Failure(ExceptionResultExtensions.FlattenException(ex));
        }
        catch (JsonException ex)
        {
            OpenIddictProviderLog.OutboundAcquireFailed(_logger, request.ClientId, ex.Message);
            return GenericResult<OutboundCredential>.Failure(ExceptionResultExtensions.FlattenException(ex));
        }
    }

    // Why: this service injects (via the scope) only the header config provider it needs — it reads
    // auth.TokenManager rows itself and selects the single enabled OpenIddict row, then joins
    // Authority + TokenEndpoint via OpenIddictTokenManagerType.ResolveTokenEndpoint (the pure string-join
    // helper every OpenIddict TypeOption/consumer shares — not a DI-registration helper, so it is not
    // in scope for the registration-class dissolution). No shared config-resolution helper: each
    // consumer that needs this config resolves it inline. If no OpenIddict config is enabled or the
    // TokenEndpoint isn't set, FAIL — never a silent default.
    private async Task<IGenericResult<string>> ResolveTokenEndpoint(
        OutboundCredentialRequest request,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var allHeaders = await scope.ServiceProvider.GetRequiredService<TokenManagerConfigurationProvider>()
            .Get(cancellationToken).ConfigureAwait(false);
        if (!allHeaders.IsSuccess)
            return GenericResult<string>.Failure(
                OpenIddictProviderLog.OutboundAcquireFailed(_logger, request.ClientId,
                    allHeaders.CurrentMessage ?? "gateway returned failure with no message"));

        var header = allHeaders.Value?
            .FirstOrDefault(c => string.Equals(c.ServiceOptionType, "OpenIddict", StringComparison.OrdinalIgnoreCase));
        if (header is null)
            return GenericResult<string>.Failure(
                OpenIddictProviderLog.OutboundAcquireFailed(_logger, request.ClientId,
                    "no enabled OpenIddict configuration is registered"));

        var typedResult = await scope.ServiceProvider.GetRequiredService<OpenIddictTokenManagerConfigurationProvider>()
            .Get(header.Id, cancellationToken).ConfigureAwait(false);
        if (!typedResult.IsSuccess || typedResult.Value is not OpenIddictTokenManagerConfiguration typed
            || string.IsNullOrEmpty(typed.TokenEndpoint))
            return GenericResult<string>.Failure(
                OpenIddictProviderLog.OutboundAcquireFailed(_logger, request.ClientId,
                    "no enabled OpenIddict configuration or token endpoint is configured"));

        return GenericResult<string>.Success(OpenIddictTokenManagerType.ResolveTokenEndpoint(typed));
    }

    private static string BuildCacheKey(OutboundCredentialRequest request)
        => $"{request.ClientId}|{string.Join(" ", request.Scopes)}|{request.Audience ?? string.Empty}";
}
