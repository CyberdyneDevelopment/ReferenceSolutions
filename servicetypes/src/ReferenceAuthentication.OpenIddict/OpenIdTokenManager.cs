using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using ReferenceAuthentication.OpenIddict.Logging;
using ReferenceAuthentication.OpenIddict.Storage;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.ExternalIdentityProviders;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Commands;
using Fdw.Services.TokenManagers.Abstractions;
using Fdw.Services.TokenManagers.Abstractions.Tokens;
using Fdw.Services.Users;
using Fdw.Services.Users.Abstractions;
using Fdw.ServiceTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict;

/// <summary>
/// The OpenIddict <see cref="ITokenManager"/> — folds the credential/issuance logic formerly split
/// across <c>OpenIddictTokenIssuanceService</c> and <c>OpenIddictTokenValidationService</c>, plus the
/// client_credentials secret check and permission baking formerly inline in
/// <see cref="Endpoints.ConnectTokenEndpointBase"/>, into the single provider-axis seam the generic
/// <see cref="Fdw.Services.TokenManagers.AuthenticationService"/> delegates to.
/// </summary>
/// <remarks>
/// <para>
/// Grant routing (<see cref="Issue"/>):
/// <list type="bullet">
///   <item><c>password</c> / <c>agent_key</c> — resolve the username to a FDW user, verify the
///     credential via <see cref="IUserCredentialService"/>, and build a thin principal.</item>
///   <item><c>external_identity</c> — map the already-validated external principal's subject to a
///     FDW user via <c>auth.ExternalIdentity</c>.</item>
///   <item><c>client_credentials</c> — resolve the <c>OAUTH_{CLIENTID}</c> secret from the header's
///     configured secret manager, compare it to the presented secret in constant time, then bake the
///     service principal's effective permission set as <c>perm</c> claims.</item>
/// </list>
/// Full FDW claim baking (tenant_id, org_id, role, perm) for the interactive paths still happens in
/// <see cref="Claims.ProcessSignInClaimsHandler"/> during OpenIddict's sign-in pipeline — this class
/// returns only the thin identity principal for those paths, exactly as
/// <c>OpenIddictTokenIssuanceService</c> did.
/// </para>
/// <para>
/// <see cref="Validate"/>/<see cref="ExtractClaims"/> verify the RS256 signature against the same
/// secret-manager-resolved key <see cref="Hosting.OpenIddictSigningKeyConfigurator"/> registers for
/// issuance. <see cref="Validate"/> additionally rejects a token whose <c>jti</c> has an unexpired row
/// in <c>auth.RevokedAccessToken</c> (written by <see cref="Invalidate"/>) — access tokens are
/// stateless RS256 JWTs (<c>DisableTokenStorage</c>), so this deny-list is the only revocation path.
/// </para>
/// </remarks>
internal sealed class OpenIdTokenManager : ITokenManager
{
    private readonly Fdw.Services.TokenManagers.TokenManagerConfiguration _header;
    private readonly OpenIddictTokenManagerConfiguration _typed;
    private readonly UserConfigurationProvider _userProvider;
    private readonly IUserCredentialService _credentialService;
    private readonly ExternalIdentityService _externalIdentityService;
    private readonly IEffectivePermissionResolver _permissionResolver;
    private readonly IFdwServiceProvider<ISecretManager, SecretManagerConfiguration> _secretManagerProvider;
    private readonly RevokedAccessTokenStore _revokedTokenStore;
    private readonly OpenIddictAuthorizationStore _authorizationStore;
    private readonly ExternalIdentityProvisionerBindingConfigurationProvider _bindingProvider;
    private readonly IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration> _provisionerProvider;
    private readonly ILogger<OpenIdTokenManager> _logger;

    /// <summary>Initializes a new instance of the <see cref="OpenIdTokenManager"/> class.</summary>
    public OpenIdTokenManager(
        Fdw.Services.TokenManagers.TokenManagerConfiguration header,
        OpenIddictTokenManagerConfiguration typed,
        UserConfigurationProvider userProvider,
        IUserCredentialService credentialService,
        ExternalIdentityService externalIdentityService,
        IEffectivePermissionResolver permissionResolver,
        IFdwServiceProvider<ISecretManager, SecretManagerConfiguration> secretManagerProvider,
        RevokedAccessTokenStore revokedTokenStore,
        OpenIddictAuthorizationStore authorizationStore,
        ILogger<OpenIdTokenManager>? logger,
        ExternalIdentityProvisionerBindingConfigurationProvider bindingProvider,
        IFdwServiceProvider<IExternalIdentityProvisioner, ExternalIdentityProvisionerConfiguration> provisionerProvider)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(typed);
        ArgumentNullException.ThrowIfNull(userProvider);
        ArgumentNullException.ThrowIfNull(credentialService);
        ArgumentNullException.ThrowIfNull(externalIdentityService);
        ArgumentNullException.ThrowIfNull(permissionResolver);
        ArgumentNullException.ThrowIfNull(secretManagerProvider);
        ArgumentNullException.ThrowIfNull(revokedTokenStore);
        ArgumentNullException.ThrowIfNull(authorizationStore);
        ArgumentNullException.ThrowIfNull(bindingProvider);
        ArgumentNullException.ThrowIfNull(provisionerProvider);
        _header = header;
        _typed = typed;
        _userProvider = userProvider;
        _credentialService = credentialService;
        _externalIdentityService = externalIdentityService;
        _permissionResolver = permissionResolver;
        _secretManagerProvider = secretManagerProvider;
        _revokedTokenStore = revokedTokenStore;
        _authorizationStore = authorizationStore;
        _bindingProvider = bindingProvider;
        _provisionerProvider = provisionerProvider;
        _logger = logger ?? NullLogger<OpenIdTokenManager>.Instance;
    }

    // ── IGenericService ────────────────────────────────────────────────────────────
    // Why: ITokenManager's surface is the direct Issue/Validate/Invalidate/ExtractClaims verb set,
    // not a command-dispatch model — there is no ITokenManagerCommand vocabulary to route through the
    // base IGenericService.Execute members. They are satisfied explicitly (mirroring how ISecretManager
    // implementations handle the same base-interface obligation) and fail loud if ever invoked, rather
    // than silently no-op.

    /// <inheritdoc cref="Fdw.Abstractions.IGenericService.Id" />
    public string Id => _header.Id.ToString();

    /// <inheritdoc cref="Fdw.Services.Abstractions.IServiceOption.Name" />
    public string Name => _header.Name;

    /// <inheritdoc cref="Fdw.Abstractions.IGenericService.ServiceType" />
    public string ServiceType => "OpenIddict";

    /// <inheritdoc cref="Fdw.Abstractions.IGenericService.IsAvailable" />
    public bool IsAvailable => true;

    Task<IGenericResult<T>> Fdw.Abstractions.IGenericService.Execute<T>(Fdw.Abstractions.IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult(GenericResult<T>.Failure(
            OpenIddictProviderLog.CommandNotDispatchableFromService(_logger, command?.CommandType ?? "(null)")));

    Task<IGenericResult> Fdw.Abstractions.IGenericService.Execute(Fdw.Abstractions.IGenericCommand command, CancellationToken cancellationToken)
        => Task.FromResult<IGenericResult>(GenericResult.Failure(
            OpenIddictProviderLog.CommandNotDispatchableFromService(_logger, command?.CommandType ?? "(null)")));

    // ── ITokenManager ──────────────────────────────────────────────────────────────

    /// <inheritdoc />
    public Task<IGenericResult<ClaimsPrincipal>> Issue(TokenIssuanceRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        OpenIddictProviderLog.IssuanceStarted(_logger, request.GrantType, request.Subject);

        return request.GrantType switch
        {
            "password" => IssueForCredential(request, isAgentKey: false, cancellationToken),
            "agent_key" => IssueForCredential(request, isAgentKey: true, cancellationToken),
            "external_identity" => IssueForExternalIdentity(request, cancellationToken),
            "client_credentials" => IssueForClientCredentials(request, cancellationToken),
            _ => Task.FromResult<IGenericResult<ClaimsPrincipal>>(
                GenericResult<ClaimsPrincipal>.Failure(OpenIddictProviderLog.UnsupportedGrantType(_logger, request.GrantType))),
        };
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ClaimsPrincipal>> Validate(string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);
        OpenIddictProviderLog.ValidationStarted(_logger);

        var validated = await ValidateSignature(token, cancellationToken).ConfigureAwait(false);
        if (!validated.IsSuccess)
            return validated.ToNewResult<ClaimsPrincipal>();

        var (principal, jti, expiresAt) = validated.Value!;

        if (jti.HasValue)
        {
            var revokedResult = await _revokedTokenStore.IsRevoked(jti.Value, cancellationToken).ConfigureAwait(false);
            if (!revokedResult.IsSuccess)
                return revokedResult.ToNewResult<ClaimsPrincipal>();
            if (revokedResult.Value)
                return GenericResult<ClaimsPrincipal>.Failure(
                    OpenIddictProviderLog.ValidationFailed(_logger, $"token jti={jti} has been revoked."));
        }

        OpenIddictProviderLog.ValidationSucceeded(_logger, principal.FindFirstValue(ClaimDefinitions.sub.Name) ?? "(none)", expiresAt);
        return GenericResult<ClaimsPrincipal>.Success(principal);
    }

    /// <inheritdoc />
    public async Task<IGenericResult> Invalidate(string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        var handler = new JsonWebTokenHandler();
        JsonWebToken jwt;
        try
        {
            jwt = handler.ReadJsonWebToken(token);
        }
        catch (ArgumentException ex)
        {
            return GenericResult.Failure(ExceptionResultExtensions.FlattenException(ex));
        }

        if (!Guid.TryParse(jwt.Id, out var jti))
            return GenericResult.Failure(
                OpenIddictProviderLog.ValidationTokenFormatError(_logger, "token carries no valid jti claim to invalidate."));

        return await _revokedTokenStore.Invalidate(jti, new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IGenericResult<ClaimsPrincipal>> ExtractClaims(string token, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(token);

        var validated = await ValidateSignature(token, cancellationToken).ConfigureAwait(false);
        return validated.IsSuccess
            ? GenericResult<ClaimsPrincipal>.Success(validated.Value.Principal)
            : validated.ToNewResult<ClaimsPrincipal>();
    }

    // ── Logout / Revoke (authorization-based session teardown) ─────────────────────

    /// <inheritdoc />
    /// <remarks>
    /// Terminates every active authorization (and, transitively, its refresh tokens) for
    /// <paramref name="subjectId"/>. Access tokens already issued remain valid until expiry
    /// (stateless JWT) unless separately invalidated (see <see cref="Invalidate"/>).
    /// </remarks>
    public async Task<IGenericResult> Logout(string subjectId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(subjectId);
        OpenIddictProviderLog.LogoutStarted(_logger, subjectId);

        var count = await _authorizationStore.RevokeBySubjectAsync(subjectId, cancellationToken).ConfigureAwait(false);

        OpenIddictProviderLog.LogoutComplete(_logger, (int)count, subjectId);
        return GenericResult.Success();
    }

    /// <summary>Explicitly revokes a single access token by invalidating its <c>jti</c>.</summary>
    public Task<IGenericResult> Revoke(string token, CancellationToken cancellationToken = default)
        => Invalidate(token, cancellationToken);

    // ── Credential grants ──────────────────────────────────────────────────────────

    private async Task<IGenericResult<ClaimsPrincipal>> IssueForCredential(
        TokenIssuanceRequest request,
        bool isAgentKey,
        CancellationToken cancellationToken)
    {
        var username = request.Subject;
        var credential = request.Credential;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(credential))
            return GenericResult<ClaimsPrincipal>.Failure(
                OpenIddictProviderLog.IssuanceMissingSubjectOrCredential(_logger, request.GrantType));

        var userResult = await _userProvider.GetUser(username, cancellationToken).ConfigureAwait(false);
        if (!userResult.IsSuccess || userResult.Value is null || !userResult.Value.IsActive)
            return GenericResult<ClaimsPrincipal>.Failure(
                OpenIddictProviderLog.UserNotFound(_logger, username));

        var user = userResult.Value;
        var secretType = isAgentKey ? "AgentKey" : "Password";
        var verifyResult = await _credentialService.Verify(user.Id, secretType, credential, cancellationToken).ConfigureAwait(false);

        // Why: Verify returns a composed ICredentialOutcome — proceed ONLY when the outcome grants
        // access (Match). Every denial outcome (NoMatch / Expired / MustChange / TooManyAttempts) and
        // any failure result fails the grant.
        var verified = verifyResult.IsSuccess && verifyResult.Value is { GrantsAccess: true };
        OpenIddictProviderLog.IssuanceBranchTrace(
            _logger, request.GrantType, username, verified,
            request.TenantId?.ToString() ?? "(default)", request.IsCrossTenant);

        if (!verified)
            return GenericResult<ClaimsPrincipal>.Failure(
                OpenIddictProviderLog.InvalidCredentials(_logger, username, request.GrantType));

        // Why: The principal returned here is thin — just sub + extra roles + optional tenant/cross-tenant.
        // Full FDW claim baking (tenant_id, org_id, role, perm) happens in ProcessSignInClaimsHandler
        // when OpenIddict fires its sign-in pipeline after the endpoint calls SignIn().
        var additionalRoles = isAgentKey
            ? (IReadOnlyList<string>)new[] { "agent" }
            : Array.Empty<string>();

        return BuildIdentityPrincipal(user.Id.ToString(), additionalRoles, request.TenantId, request.OrgId, request.IsCrossTenant);
    }

    private async Task<IGenericResult<ClaimsPrincipal>> IssueForExternalIdentity(
        TokenIssuanceRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ExternalPrincipal is null)
            return GenericResult<ClaimsPrincipal>.Failure(
                OpenIddictProviderLog.IssuanceFailed(_logger, "external_identity", "(null)", "ExternalPrincipal must not be null for external_identity grant."));

        var provider = request.ExternalPrincipal.FindFirstValue("iss")
            ?? request.ExternalPrincipal.FindFirstValue("provider")
            ?? string.Empty;
        var externalSubject = request.ExternalPrincipal.FindFirstValue(ClaimDefinitions.sub.Name)
            ?? request.ExternalPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? string.Empty;

        if (string.IsNullOrEmpty(provider) || string.IsNullOrEmpty(externalSubject))
            return GenericResult<ClaimsPrincipal>.Failure(
                OpenIddictProviderLog.IssuanceFailed(_logger, "external_identity", "(unknown)", "ExternalPrincipal missing iss/provider or sub claim."));

        var lookupResult = await _externalIdentityService
            .FindUserId(provider, externalSubject, cancellationToken)
            .ConfigureAwait(false);

        if (!lookupResult.IsSuccess)
            return lookupResult.ToNewResult<ClaimsPrincipal>();

        if (lookupResult.Value is null)
            return await ProvisionOrFail(provider, externalSubject, request, cancellationToken).ConfigureAwait(false);

        return BuildIdentityPrincipal(lookupResult.Value.Value.ToString(), Array.Empty<string>(), request.TenantId, request.OrgId, request.IsCrossTenant);
    }

    // Why: consulted ONLY on a FindUserId miss, BEFORE returning ExternalIdentityNotFound. Resolves the
    // provisioner to use for THIS (tenant, external provider) pair via
    // ExternalIdentityProvisionerBindingConfigurationProvider.ResolveProvisionerName — an absent binding
    // is byte-identical to the old default-OFF behavior (ExternalIdentityNotFound). A resolved binding
    // whose provisioner name doesn't resolve to a registered IExternalIdentityProvisioner, or whose
    // Provision() call fails, is propagated as a hard error — NEVER masked as "not found" (NO
    // FALLBACKS — a provisioner that was selected and then failed is a distinct outcome from "no
    // provisioner configured for this provider").
    private async Task<IGenericResult<ClaimsPrincipal>> ProvisionOrFail(
        string provider, string externalSubject, TokenIssuanceRequest request, CancellationToken cancellationToken)
    {
        OpenIddictProviderLog.ProvisionerBindingLookupStarted(_logger, provider, request.TenantId?.ToString() ?? "(global)");

        var bindingResult = await _bindingProvider
            .ResolveProvisionerName(request.TenantId, provider, cancellationToken)
            .ConfigureAwait(false);
        if (!bindingResult.IsSuccess)
            return bindingResult.ToNewResult<ClaimsPrincipal>();

        var provisionerName = bindingResult.Value;
        if (string.IsNullOrEmpty(provisionerName))
            return GenericResult<ClaimsPrincipal>.Failure(
                OpenIddictProviderLog.ExternalIdentityNotFound(_logger, provider, externalSubject));

        var provisionerResult = await _provisionerProvider.Get(provisionerName, cancellationToken).ConfigureAwait(false);
        if (!provisionerResult.IsSuccess || provisionerResult.Value is null)
            return GenericResult<ClaimsPrincipal>.Failure(
                OpenIddictProviderLog.ProvisionerResolutionFailed(_logger, provisionerName, provider,
                    provisionerResult.CurrentMessage ?? "provisioner could not be resolved."));

        var provisionResult = await provisionerResult.Value
            .Provision(provider, externalSubject, request.ExternalPrincipal!, cancellationToken)
            .ConfigureAwait(false);

        if (!provisionResult.IsSuccess)
        {
            OpenIddictProviderLog.ExternalIdentityProvisioningFailed(_logger, provisionerName, provider, externalSubject,
                provisionResult.CurrentMessage ?? "provisioning failed.");
            return provisionResult.ToNewResult<ClaimsPrincipal>();
        }

        OpenIddictProviderLog.ExternalIdentityProvisioned(_logger, provider, externalSubject, provisionResult.Value);
        return BuildIdentityPrincipal(provisionResult.Value.ToString(), Array.Empty<string>(), request.TenantId, request.OrgId, request.IsCrossTenant);
    }

    // Why: Creates a thin ClaimsPrincipal carrying sub + extra roles + optional tenant/cross-tenant context.
    // The tenant_id/org_id/cross_tenant claims are needed on the thin principal so that
    // ProcessSignInClaimsHandler can pick them up during the sign-in pipeline and pass them
    // to DefaultPrincipalResolver. Full FDW claim baking (role, perm) stays in ProcessSignInClaimsHandler.
    private static IGenericResult<ClaimsPrincipal> BuildIdentityPrincipal(
        string userId,
        IReadOnlyList<string> additionalRoles,
        Guid? tenantId,
        Guid? orgId,
        bool isCrossTenant)
    {
        var claims = new List<Claim>(capacity: 3 + additionalRoles.Count)
        {
            new(ClaimDefinitions.sub.Name, userId),
        };

        // Why: Mutually exclusive — cross-tenant tokens have no single tenantId.
        if (isCrossTenant)
            claims.Add(new Claim(ClaimDefinitions.crossTenant.Name, "true"));
        else if (tenantId.HasValue)
            claims.Add(new Claim(ClaimDefinitions.tenantId.Name, tenantId.Value.ToString()));

        if (!isCrossTenant && orgId.HasValue)
            claims.Add(new Claim(ClaimDefinitions.orgId.Name, orgId.Value.ToString()));

        // Why: emit under the plural "roles" claim — ProcessSignInClaimsHandler.CollectRoles reads
        // ClaimDefinitions.roles.Name, so adding them under singular "role" silently dropped the extra roles.
        foreach (var role in additionalRoles)
            claims.Add(new Claim(ClaimDefinitions.roles.Name, role));

        var identity = new ClaimsIdentity(claims, authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        return GenericResult<ClaimsPrincipal>.Success(new ClaimsPrincipal(identity));
    }

    // ── client_credentials ────────────────────────────────────────────────────────

    // Why: Machine-to-machine tokens carry the client_id as subject; there is no interactive user, so
    // ProcessSignInClaimsHandler skips this grant (ClientOnlyGrantTypes set). This is FDW's own
    // client-secret check — the seeded confidential-client row is never version-on-written with a
    // manager-hashed secret, so OpenIddict's own client authentication is not what protects this grant;
    // FDW resolves the shared secret from the header's configured secret manager under the
    // OAUTH_{CLIENTID} convention and compares it in constant time. NO FALLBACKS: missing config,
    // missing secret, or a mismatch are all fail-loud, never a defaulted/skipped check.
    private async Task<IGenericResult<ClaimsPrincipal>> IssueForClientCredentials(
        TokenIssuanceRequest request,
        CancellationToken cancellationToken)
    {
        var clientId = request.Subject;
        var presentedSecret = request.Credential;

        if (string.IsNullOrEmpty(clientId))
            return GenericResult<ClaimsPrincipal>.Failure(
                OpenIddictProviderLog.IssuanceMissingSubjectOrCredential(_logger, request.GrantType));

        if (string.IsNullOrEmpty(_header.SecretManagerName))
            return GenericResult<ClaimsPrincipal>.Failure(
                OpenIddictProviderLog.IssuanceFailed(_logger, request.GrantType, clientId,
                    $"TokenManager configuration '{_header.Name}' has no SecretManagerName; client_credentials cannot be validated."));

        var secretKey = "OAUTH_" + clientId.ToUpperInvariant().Replace(".", string.Empty, StringComparison.Ordinal);

        var managerResult = await _secretManagerProvider.Get(_header.SecretManagerName, cancellationToken).ConfigureAwait(false);
        if (!managerResult.IsSuccess || managerResult.Value is null)
            return GenericResult<ClaimsPrincipal>.Failure(
                OpenIddictProviderLog.IssuanceFailed(_logger, request.GrantType, clientId,
                    $"secret manager '{_header.SecretManagerName}' could not be resolved."));

        var secretResult = await managerResult.Value
            .Execute(GetSecretManagerCommand.Latest(container: null, secretKey: secretKey), cancellationToken)
            .ConfigureAwait(false);

        if (!secretResult.IsSuccess || secretResult.Value is not SecretValue secret)
            return GenericResult<ClaimsPrincipal>.Failure(
                OpenIddictProviderLog.IssuanceFailed(_logger, request.GrantType, clientId,
                    $"no client secret configured under key '{secretKey}'."));

        bool matches;
        using (secret)
        {
            matches = !string.IsNullOrEmpty(presentedSecret)
                && SecretEquals(secret.GetStringValue(), presentedSecret);
        }

        if (!matches)
            return GenericResult<ClaimsPrincipal>.Failure(
                OpenIddictProviderLog.InvalidCredentials(_logger, clientId, request.GrantType));

        OpenIddictProviderLog.ClientCredentialsClaimBakeStarted(_logger, clientId);

        var identity = new ClaimsIdentity(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        identity.AddClaim(ClaimDefinitions.sub.Name, clientId);

        // Why: service principals operate outside any tenant/org context — they hold global
        // (TenantId IS NULL) role grants. Resolve with no tenant/org so only the global tier
        // contributes, mirroring how the EffectivePermissionResolver treats global grants.
        var permResult = await _permissionResolver.Resolve(
            clientId, tenantId: null, orgId: null, isGlobalTenant: false, cancellationToken).ConfigureAwait(false);

        if (!permResult.IsSuccess)
        {
            OpenIddictProviderLog.ClientCredentialsClaimBakeFailed(_logger, clientId, permResult.CurrentMessage!);
            return GenericResult<ClaimsPrincipal>.Failure(
                OpenIddictProviderLog.InvalidCredentials(_logger, clientId, request.GrantType));
        }

        BakePermissionClaims(identity, permResult.Value!);
        OpenIddictProviderLog.ClientCredentialsClaimBaked(_logger, clientId, permResult.Value!.Count);

        return GenericResult<ClaimsPrincipal>.Success(new ClaimsPrincipal(identity));
    }

    // Why: constant-time comparison over UTF8 bytes so a mismatched secret's length/content cannot be
    // inferred by timing. A length mismatch still short-circuits before the timing-safe compare — the
    // same accepted trade-off FixedTimeEquals callers across the codebase make.
    private static bool SecretEquals(string expected, string presented)
    {
        var expectedBytes = System.Text.Encoding.UTF8.GetBytes(expected);
        var presentedBytes = System.Text.Encoding.UTF8.GetBytes(presented);
        return expectedBytes.Length == presentedBytes.Length
            && CryptographicOperations.FixedTimeEquals(expectedBytes, presentedBytes);
    }

    // Why: Mirror DefaultPrincipalResolver/ProcessSignInClaimsHandler — 'perm' is a scalar claim
    // (one claim per permission) ridden on the AccessToken destination so DefaultAuthorizationService
    // can read it per-request. The resource-server policy ('pipelines:execute' etc.) enforces against
    // these perm claims; the service principal's roles are not part of that contract and a role set is
    // not produced by IEffectivePermissionResolver, so no 'roles' claim is fabricated here.
    private static void BakePermissionClaims(ClaimsIdentity identity, IReadOnlyCollection<string> permissions)
    {
        var permDefinition = ClaimDefinitions.ByName(ClaimDefinitions.perm.Name);
        foreach (var permission in permissions)
        {
            var permClaim = new Claim(ClaimDefinitions.perm.Name, permission);
            SetClaimDestinations(permClaim, permDefinition);
            identity.AddClaim(permClaim);
        }
    }

    // Why: maps the claim definition's FDW token destinations onto the OpenIddict claim so the
    // baked perm claims are written to the access token the resource server validates.
    private static void SetClaimDestinations(Claim claim, IClaimDefinition definition)
    {
        if (definition == ClaimDefinitions.NotFound)
        {
            claim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
            return;
        }

        var mapped = new string[definition.Destinations.Count];
        for (var i = 0; i < definition.Destinations.Count; i++)
            mapped[i] = string.Equals(definition.Destinations[i], TokenDestinations.IdentityToken, StringComparison.Ordinal)
                ? OpenIddictConstants.Destinations.IdentityToken
                : OpenIddictConstants.Destinations.AccessToken;
        claim.SetDestinations(mapped);
    }

    // ── Signature validation (shared by Validate / ExtractClaims) ──────────────────

    private async Task<IGenericResult<(ClaimsPrincipal Principal, Guid? Jti, DateTimeOffset ExpiresAt)>> ValidateSignature(
        string token, CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(_typed.Authority, UriKind.Absolute, out var issuer)
            || string.IsNullOrEmpty(_header.SecretManagerName)
            || string.IsNullOrEmpty(_header.SecretKeyName))
            return GenericResult<(ClaimsPrincipal, Guid?, DateTimeOffset)>.Failure(
                OpenIddictProviderLog.ValidationSigningKeyUnavailable(_logger));

        var keyResult = await ResolveSigningKey(cancellationToken).ConfigureAwait(false);
        if (!keyResult.IsSuccess)
            return keyResult.ToNewResult<(ClaimsPrincipal, Guid?, DateTimeOffset)>();
        var key = keyResult.Value!;

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer.AbsoluteUri,
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            IssuerSigningKey = key,
            ValidAlgorithms = new[] { SecurityAlgorithms.RsaSha256 },
            // Why: FDW bakes role names under the "roles" claim (plural, JSON array) — match that
            // so User.IsInRole()/[Authorize(Roles=)] resolve against the actual token claim.
            RoleClaimType = ClaimDefinitions.roles.Name,
            NameClaimType = ClaimDefinitions.sub.Name,
        };

        var handler = new JsonWebTokenHandler { MapInboundClaims = false };
        try
        {
            var result = await handler.ValidateTokenAsync(token, validationParameters).ConfigureAwait(false);
            if (!result.IsValid)
                return GenericResult<(ClaimsPrincipal, Guid?, DateTimeOffset)>.Failure(
                    OpenIddictProviderLog.ValidationFailed(_logger, result.Exception!.Message));

            var principal = new ClaimsPrincipal(result.ClaimsIdentity);
            var jti = Guid.TryParse(principal.FindFirstValue("jti"), out var parsedJti) ? parsedJti : (Guid?)null;
            var expiresAt = result.SecurityToken is JsonWebToken jwt
                ? new DateTimeOffset(jwt.ValidTo, TimeSpan.Zero)
                : DateTimeOffset.UtcNow;

            return GenericResult<(ClaimsPrincipal, Guid?, DateTimeOffset)>.Success((principal, jti, expiresAt));
        }
        catch (SecurityTokenException ex)
        {
            OpenIddictProviderLog.ValidationFailed(_logger, ex.Message);
            return GenericResult<(ClaimsPrincipal, Guid?, DateTimeOffset)>.Failure(ExceptionResultExtensions.FlattenException(ex));
        }
    }

    private async Task<IGenericResult<RsaSecurityKey>> ResolveSigningKey(CancellationToken cancellationToken)
    {
        OpenIddictProviderLog.SigningKeyLoadStarted(_logger, _header.SecretManagerName!, _header.SecretKeyName!);

        var managerResult = await _secretManagerProvider.Get(_header.SecretManagerName!, cancellationToken).ConfigureAwait(false);
        if (!managerResult.IsSuccess || managerResult.Value is null)
            return GenericResult<RsaSecurityKey>.Failure(
                OpenIddictProviderLog.SigningKeyManagerNotFound(_logger, _header.SecretManagerName!));

        var secretResult = await managerResult.Value
            .Execute(GetSecretManagerCommand.Latest(container: null, secretKey: _header.SecretKeyName!), cancellationToken)
            .ConfigureAwait(false);
        if (!secretResult.IsSuccess)
        {
            var reason = secretResult.CurrentMessage ?? string.Empty;
            return GenericResult<RsaSecurityKey>.Failure(
                OpenIddictProviderLog.SigningKeyLoadFailed(
                    _logger, _header.SecretManagerName!, _header.SecretKeyName!,
                    string.IsNullOrEmpty(reason) ? "secret manager returned a failure with no message" : reason));
        }

        if (secretResult.Value is not SecretValue secret)
            return GenericResult<RsaSecurityKey>.Failure(
                OpenIddictProviderLog.SigningKeyMissing(_logger, _header.SecretManagerName!, _header.SecretKeyName!));

        try
        {
            using (secret)
            {
                const string keyId = "fdw-rs256-1";
                var rsa = RSA.Create();
                rsa.ImportFromPem(secret.GetStringValue().AsSpan());
                var key = new RsaSecurityKey(rsa) { KeyId = keyId };
                OpenIddictProviderLog.SigningKeyLoaded(_logger, _header.SecretManagerName!, _header.SecretKeyName!, keyId);
                return GenericResult<RsaSecurityKey>.Success(key);
            }
        }
        catch (CryptographicException ex)
        {
            OpenIddictProviderLog.SigningKeyParseFailed(_logger, ex, _header.SecretKeyName!, ex.Message);
            return GenericResult<RsaSecurityKey>.Failure(ExceptionResultExtensions.FlattenException(ex));
        }
        catch (ArgumentException ex)
        {
            OpenIddictProviderLog.SigningKeyParseFailed(_logger, ex, _header.SecretKeyName!, ex.Message);
            return GenericResult<RsaSecurityKey>.Failure(ExceptionResultExtensions.FlattenException(ex));
        }
    }
}
