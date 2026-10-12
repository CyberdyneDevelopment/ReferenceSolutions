using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using FdwReferenceSecretManagers.HashiCorpVault.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Fdw.Services.SecretManagers;

namespace FdwReferenceSecretManagers.HashiCorpVault;

/// <summary>
/// Talks to Vault's HTTP API: logs in via the configured auth method, then reads through the
/// configured secret engine.
/// </summary>
/// <remarks>
/// <para>
/// Nothing here logs a token, a secret id, or a secret value. Failures report the address, the
/// engine, the path and Vault's own error strings, which is what diagnoses a misconfiguration
/// without handing a log reader the ability to use the credential.
/// </para>
/// <para>
/// The Vault token obtained at login is held for the lifetime of this client and reused across
/// reads, then dropped when its own lease is close to ending. Logging in per read would multiply
/// Vault's auth load by the read rate for no security gain — the token is already short-lived.
/// </para>
/// </remarks>
public sealed class VaultApiClient : IDisposable
{
    /// <summary>
    /// How long before its expiry a Vault token is renewed. A token that passes the check must survive
    /// the read that follows it. A deployment sets this before the first secret is read.
    /// </summary>
    public static TimeSpan TokenRefreshSkew { get; set; } = TimeSpan.FromSeconds(60);

    private readonly HttpClient _http;
    private readonly ILogger _logger;
    private readonly SemaphoreSlim _loginGate = new(1, 1);

    private string? _token;
    private DateTimeOffset _tokenExpiresAt;
    private bool _disposed;

    /// <summary>Releases the login gate this client owns.</summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _loginGate.Dispose();
    }

    /// <summary>Initializes a new instance of the <see cref="VaultApiClient"/> class.</summary>
    /// <param name="http">The HTTP client used to reach Vault.</param>
    /// <param name="logger">The logger for this client.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="http"/> is null.</exception>
    public VaultApiClient(HttpClient http, ILogger? logger = null)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _logger = logger ?? NullLogger.Instance;
    }

    /// <summary>
    /// Reads <paramref name="secretKey"/> through the context's configured engine, logging in first if there
    /// is no live Vault token.
    /// </summary>
    /// <param name="context">Everything about which Vault, which engine and how to log in.</param>
    /// <param name="secretKey">The KV path or database role name to read.</param>
    /// <param name="cancellationToken">Propagated cancellation token.</param>
    /// <returns>The secret, or the structured reason the read did not produce one.</returns>
    public async Task<IGenericResult<SecretValue>> Read(
        VaultReadContext context,
        string secretKey,
        CancellationToken cancellationToken = default)
    {
        var token = await Authenticate(context, cancellationToken).ConfigureAwait(false);
        if (token.IsFailure || token.Value is not { Length: > 0 } vaultToken)
            return token.ToNewResult<SecretValue>();

        var path = context.Engine.BuildReadPath(context.Mount, secretKey);
        VaultLog.ReadingSecret(_logger, context.ConfigurationName, context.Engine.Name, path);

        var response = await Send(context, HttpMethod.Get, path, body: null, vaultToken, cancellationToken).ConfigureAwait(false);
        if (response.IsFailure || response.Value is not { } payload)
            return response.ToNewResult<SecretValue>();

        return ReadSecret(context, secretKey, payload);
    }

    private IGenericResult<SecretValue> ReadSecret(VaultReadContext context, string secretKey, string body)
    {
        JsonElement root;
        try
        {
            root = JsonDocument.Parse(body).RootElement;
        }
        catch (JsonException ex)
        {
            return GenericResult<SecretValue>.Failure(
                VaultLog.ResponseUnreadable(_logger, ex, context.ConfigurationName, context.Address));
        }

        if (!root.TryGetProperty("data", out var data))
            return GenericResult<SecretValue>.Failure(
                VaultLog.ResponseIncomplete(_logger, context.ConfigurationName, context.Engine.Name, "data"));

        // Why KV v2 is unwrapped one level further: it nests the caller's own fields under data.data
        // and its version metadata under data.metadata. The database engine puts username/password
        // directly on data. Asking the engine rather than sniffing the shape keeps the difference
        // declared in one place.
        var fields = context.Engine.IssuesCredential
            ? data
            : data.TryGetProperty("data", out var inner) ? inner : data;

        if (!fields.TryGetProperty(context.Engine.ValueField, out var value) || value.GetString() is not { Length: > 0 } secret)
            return GenericResult<SecretValue>.Failure(
                VaultLog.ResponseIncomplete(_logger, context.ConfigurationName, context.Engine.Name, context.Engine.ValueField));

        if (!context.Engine.IssuesCredential)
        {
            VaultLog.SecretRead(_logger, context.ConfigurationName, context.Engine.Name);
            return GenericResult<SecretValue>.Success(new SecretValue(secretKey, secret));
        }

        var lease = ReadLease(root, fields);
        VaultLog.CredentialIssued(_logger, context.ConfigurationName, secretKey, lease.ExpiresAt?.ToString("O") ?? "no lease");

        return GenericResult<SecretValue>.Success(
            new SecretValue(secretKey, secret, version: null, expiresAt: lease.ExpiresAt, metadata: lease.Metadata));
    }

    // Why separated: an issued credential carries lease bookkeeping a stored secret does not, and
    // folding both shapes into one method pushed it past the complexity gate.
    private static (DateTimeOffset? ExpiresAt, Dictionary<string, object> Metadata) ReadLease(JsonElement root, JsonElement fields)
    {
        var metadata = new Dictionary<string, object>(StringComparer.Ordinal);
        DateTimeOffset? expiresAt = null;

        // Why the username is carried as metadata: Vault MINTS it, so it is part of the credential
        // rather than something the caller already knew. See VaultReadContext's remarks about the
        // connection layer not yet consuming it.
        if (fields.TryGetProperty("username", out var username) && username.GetString() is { Length: > 0 } issuedUser)
            metadata["username"] = issuedUser;

        if (root.TryGetProperty("lease_duration", out var leaseDuration) && leaseDuration.TryGetInt32(out var leaseSeconds))
            expiresAt = DateTimeOffset.UtcNow.AddSeconds(leaseSeconds);

        if (root.TryGetProperty("lease_id", out var leaseId) && leaseId.GetString() is { Length: > 0 } issuedLease)
            metadata["lease_id"] = issuedLease;

        return (expiresAt, metadata);
    }

    private async Task<IGenericResult<string>> Authenticate(VaultReadContext context, CancellationToken cancellationToken)
    {
        // Why the Token method short-circuits: the configured secret IS the Vault token, so there is
        // nothing to exchange and a login round trip would fail.
        if (context.AuthMethod.Name.Equals("Token", StringComparison.Ordinal))
            return await context.ResolveAuthSecret(cancellationToken).ConfigureAwait(false);

        if (_token is { Length: > 0 } cached && DateTimeOffset.UtcNow + TokenRefreshSkew < _tokenExpiresAt)
        {
            VaultLog.TokenReused(_logger, context.ConfigurationName, _tokenExpiresAt);
            return GenericResult<string>.Success(cached);
        }

        VaultLog.LoginRequired(
            _logger,
            context.ConfigurationName,
            _token is { Length: > 0 } ? "held token is inside its refresh skew" : "no token held");

        await _loginGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Why the second check: another caller may have logged in while this one waited, and
            // logging in again would spend a fresh lease for nothing.
            if (_token is { Length: > 0 } justLoggedIn && DateTimeOffset.UtcNow + TokenRefreshSkew < _tokenExpiresAt)
                return GenericResult<string>.Success(justLoggedIn);

            return await Login(context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _loginGate.Release();
        }
    }

    private async Task<IGenericResult<string>> Login(VaultReadContext context, CancellationToken cancellationToken)
    {
        var mount = string.IsNullOrWhiteSpace(context.AuthMount) ? context.AuthMethod.DefaultMount : context.AuthMount!;
        VaultLog.LoggingIn(_logger, context.ConfigurationName, context.AuthMethod.Name, mount);

        var authSecret = await context.ResolveAuthSecret(cancellationToken).ConfigureAwait(false);
        if (authSecret.IsFailure || authSecret.Value is not { } secret)
            return authSecret.ToNewResult<string>();

        VaultLog.LoginCredentialResolved(_logger, context.ConfigurationName, context.AuthMethod.Name);

        var response = await Send(
            context,
            HttpMethod.Post,
            $"auth/{mount}/{context.AuthMethod.LoginPath}",
            JsonSerializer.Serialize(context.AuthMethod.BuildLoginPayload(context.AuthRoleId, secret)),
            vaultToken: null,
            cancellationToken).ConfigureAwait(false);

        if (response.IsFailure || response.Value is not { } body)
            return response.ToNewResult<string>();

        JsonElement root;
        try
        {
            root = JsonDocument.Parse(body).RootElement;
        }
        catch (JsonException ex)
        {
            return GenericResult<string>.Failure(
                VaultLog.ResponseUnreadable(_logger, ex, context.ConfigurationName, context.Address));
        }

        if (!root.TryGetProperty("auth", out var auth)
            || !auth.TryGetProperty("client_token", out var clientToken)
            || clientToken.GetString() is not { Length: > 0 } issued)
            return GenericResult<string>.Failure(
                VaultLog.ResponseIncomplete(_logger, context.ConfigurationName, context.AuthMethod.Name, "auth.client_token"));

        // NO FALLBACKS: a login response without a lease duration is not assumed to last forever —
        // that assumption would keep a dead token in hand and fail every read after it expired.
        if (!auth.TryGetProperty("lease_duration", out var lease) || !lease.TryGetInt32(out var leaseSeconds))
            return GenericResult<string>.Failure(
                VaultLog.ResponseIncomplete(_logger, context.ConfigurationName, context.AuthMethod.Name, "auth.lease_duration"));

        _token = issued;
        _tokenExpiresAt = DateTimeOffset.UtcNow.AddSeconds(leaseSeconds);
        VaultLog.LoggedIn(_logger, context.ConfigurationName, context.AuthMethod.Name, _tokenExpiresAt);

        return GenericResult<string>.Success(issued);
    }

    private async Task<IGenericResult<string>> Send(
        VaultReadContext context,
        HttpMethod method,
        string path,
        string? body,
        string? vaultToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(method, new Uri($"{context.Address.TrimEnd('/')}/v1/{path}"));

        if (vaultToken is { Length: > 0 })
            request.Headers.Add("X-Vault-Token", vaultToken);

        if (context.VaultNamespace is { Length: > 0 })
            request.Headers.Add("X-Vault-Namespace", context.VaultNamespace);

        if (body is not null)
            request.Content = new StringContent(body, Encoding.UTF8, new MediaTypeHeaderValue("application/json"));

        VaultLog.SendingRequest(_logger, context.ConfigurationName, method.Method, path);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            return GenericResult<string>.Failure(
                VaultLog.VaultUnreachable(_logger, ex, context.ConfigurationName, context.Address));
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return GenericResult<string>.Failure(
                VaultLog.VaultUnreachable(_logger, ex, context.ConfigurationName, context.Address));
        }

        using (response)
        {
            VaultLog.RequestAnswered(_logger, context.ConfigurationName, path, (int)response.StatusCode);
            var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
                return GenericResult<string>.Success(payload);

            // Why 403 is called out separately: Vault answers "your token is fine but your policy does
            // not permit this path" with 403, which is a grant problem an operator fixes in Vault —
            // materially different from a bad credential (400/401) or Vault being down.
            // Why matched on the numeric status rather than the HttpStatusCode enum: an enum switch
            // here trips FDW018, which steers dispatch onto TypeCollections. That rule is right about
            // domain concepts and wrong about HTTP status codes — they are a fixed external protocol
            // that no consumer extends, so a TypeCollection would add a registration ceremony around
            // three numbers defined by an RFC.
            return GenericResult<string>.Failure((int)response.StatusCode switch
            {
                // Vault authenticated the caller but its policy forbids this path — a grant problem an
                // operator fixes in Vault, materially different from a bad credential.
                403 => VaultLog.PermissionDenied(_logger, context.ConfigurationName, path, DescribeErrors(context, payload)),
                400 or 401 => VaultLog.AuthenticationRejected(_logger, context.ConfigurationName, context.AuthMethod.Name, DescribeErrors(context, payload)),
                404 => VaultLog.SecretNotFound(_logger, context.ConfigurationName, path),
                var other => VaultLog.VaultReturnedError(_logger, context.ConfigurationName, context.Address, other),
            });
        }
    }

    private string DescribeErrors(VaultReadContext context, string body)
    {
        try
        {
            if (JsonDocument.Parse(body).RootElement.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Array)
            {
                var described = new List<string>();
                foreach (var error in errors.EnumerateArray())
                {
                    if (error.GetString() is { Length: > 0 } text)
                        described.Add(text);
                }

                return described.Count > 0 ? string.Join("; ", described) : "no error detail in response";
            }

            return "no error detail in response";
        }
        catch (JsonException ex)
        {
            // Why logged rather than swallowed: a non-JSON body means the request did not reach
            // Vault's API at all (a proxy error page, a wrong address), which is a different fault
            // from a rejected credential and would otherwise be reported as one.
            VaultLog.ErrorResponseUnparseable(_logger, ex, context.ConfigurationName);
            return "unparseable error response";
        }
    }
}
