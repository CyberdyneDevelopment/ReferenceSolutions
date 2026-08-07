using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions;
using ReferenceAuthentication.OpenIddict.Logging;
using Fdw.Services.ExternalIdentityProviders;
using Fdw.Services.TokenManagers.Abstractions.Tokens;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
// Why: Microsoft.AspNetCore.Authentication also declares an IAuthenticationService — alias the FDW
// generic authN seam explicitly so the two never collide (CS0104).
using IAuthenticationService = Fdw.Services.TokenManagers.Abstractions.IAuthenticationService;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Endpoints;

/// <summary>
/// FDW-owned <c>/connect/token</c> endpoint for the OpenIddict auth server.
/// Implemented as a FastEndpoints <see cref="EndpointWithoutRequest"/> (the FDW way) so
/// applications do NOT write any auth token code. It reads the OpenIddict request directly
/// from <c>HttpContext</c> rather than binding a typed request body.
///
/// Why FastEndpoints (not MVC): registering this assembly as an MVC ApplicationPart pollutes
/// the shared ApplicationPartManager that FastEndpoints scans, producing "More than one
/// validator was found" at startup in FastEndpoints-native consumer apps. Every other FDW
/// endpoint is FastEndpoints; this is now consistent with them.
///
/// Grant type routing — every grant funnels through the single generic
/// <see cref="IAuthenticationService"/> seam (→ the active <c>ITokenManager</c>'s
/// <c>Issue</c>, which now owns ALL credential/secret validation and permission baking):
/// <list type="bullet">
///   <item><c>password</c> / <c>agent_key</c> / <c>external_identity</c> / <c>client_credentials</c> —
///     builds a <see cref="TokenIssuanceRequest"/>, calls <see cref="IAuthenticationService.Authenticate(TokenIssuanceRequest, CancellationToken)"/>,
///     then signs in via OpenIddict. <see cref="Claims.ProcessSignInClaimsHandler"/> bakes the full
///     FDW claim set for the interactive paths; <c>client_credentials</c> already carries its baked
///     'perm' claims from the token manager.
///   </item>
///   <item><c>refresh_token</c> — Re-authenticates the stored principal and signs in with
///     fresh FDW claims (ProcessSignInClaimsHandler re-resolves perms on each refresh).</item>
///   <item><c>authorization_code</c> — Re-authenticates the code-exchange principal and signs in.</item>
/// </list>
/// OpenIddict mints the RS256 access token and manages refresh token rotation.
/// </summary>
public abstract class ConnectTokenEndpointBase : EndpointWithoutRequest
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ExternalIdentityProviderResolver? _externalIdentityProviderResolver;
    private readonly ILogger<ConnectTokenEndpointBase> _logger;

    /// <summary>Initializes a new instance of the <see cref="ConnectTokenEndpointBase"/> class.</summary>
    /// <param name="authenticationService">The generic, provider-agnostic authN service.</param>
    /// <param name="externalIdentityProviderResolver">
    /// Resolves the <c>IExternalIdentityProvider</c> for the <c>external_identity</c> grant, or
    /// <see langword="null"/> when no external-identity domain is registered — in which case that grant
    /// is simply not supported. Every other grant is unaffected.
    /// </param>
    /// <param name="logger">The optional logger.</param>
    // Why: abstract base — FastEndpoints does not discover endpoints that live inside a referenced
    // package assembly (only the entry/app assembly is scanned). Consumer apps declare a thin concrete
    // subclass in their OWN assembly so FE discovers and maps /connect/token; all logic stays here.
    //
    // Why the resolver is OPTIONAL (FDW-624): it used to be a required dependency, so an app that never
    // wanted external-IdP login still had to register the external-identity domain or /connect/token
    // failed at DI construction — taking the password grant down with it. External login is a capability
    // a consumer opts into by referencing an ExternalIdentityProviderTypes option; its absence must
    // degrade to "that one grant is unsupported", never to "the token endpoint cannot start".
    // This is NOT a fallback value: nothing is defaulted or guessed. A request that actually asks for
    // external_identity when the capability is absent fails loud with a structured message (see
    // MapExternalIdentityGrant); the absence is only ever reported, never papered over.
    protected ConnectTokenEndpointBase(
        IAuthenticationService authenticationService,
        ExternalIdentityProviderResolver? externalIdentityProviderResolver,
        ILogger<ConnectTokenEndpointBase>? logger)
    {
        ArgumentNullException.ThrowIfNull(authenticationService);
        _authenticationService = authenticationService;
        _externalIdentityProviderResolver = externalIdentityProviderResolver;
        _logger = logger ?? NullLogger<ConnectTokenEndpointBase>.Instance;
    }

    /// <inheritdoc/>
    public override void Configure()
    {
        Post("/connect/token");
        AllowAnonymous();

        // Why: The token endpoint consumes application/x-www-form-urlencoded. EndpointWithoutRequest
        // does not model-bind a body, and FastEndpoints does not apply MVC antiforgery, so the form
        // POST is accepted without any antiforgery handling. AllowFormData declares the urlencoded
        // content type for OpenAPI/routing without introducing a typed request model.
        AllowFormData(urlEncoded: true);
    }

    /// <summary>Handles all token endpoint grant types.</summary>
    /// <inheritdoc/>
    public override async Task HandleAsync(CancellationToken ct)
    {
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        // Why: GrantType is validated non-null by OpenIddict before this handler fires; ! is safe.
        OpenIddictProviderLog.TokenEndpointStarted(_logger, request.GrantType!);

        var result = await RouteGrant(request).ConfigureAwait(false);
        await Send.ResultAsync(result).ConfigureAwait(false);
    }

    private async Task<IResult> RouteGrant(OpenIddictRequest request)
    {
        if (IsCustomGrant(request) || request.IsClientCredentialsGrantType())
            return await HandleAuthenticatedGrant(request).ConfigureAwait(false);

        if (request.IsRefreshTokenGrantType() || request.IsAuthorizationCodeGrantType())
            return await HandleCodeOrRefresh().ConfigureAwait(false);

        OpenIddictProviderLog.TokenEndpointUnsupportedGrant(_logger, request.GrantType!);
        return Microsoft.AspNetCore.Http.Results.Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.UnsupportedGrantType,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = $"The grant type '{request.GrantType}' is not supported.",
            }),
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
    }

    // ── password / agent_key / external_identity / client_credentials ──────────────

    private async Task<IResult> HandleAuthenticatedGrant(OpenIddictRequest request)
    {
        var issuanceRequestResult = await MapToIssuanceRequest(request, HttpContext.RequestAborted).ConfigureAwait(false);
        if (!issuanceRequestResult.IsSuccess)
        {
            OpenIddictProviderLog.TokenEndpointCredentialFailed(
                _logger, request.GrantType!, request.Username ?? request.ClientId);

            return ForbidInvalidGrant();
        }

        var principalResult = await _authenticationService.Authenticate(issuanceRequestResult.Value!, HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (!principalResult.IsSuccess)
        {
            OpenIddictProviderLog.TokenEndpointCredentialFailed(
                _logger, request.GrantType!, request.Username ?? request.ClientId);

            return ForbidInvalidGrant();
        }

        var principal = principalResult.Value!;
        principal.SetScopes(request.GetScopes());

        // Why: SignIn hands the principal to OpenIddict's server pipeline.
        // ProcessSignInClaimsHandler fires there and bakes the full FDW claim set
        // (tenant_id, org_id, role, perm) for the interactive paths — NOT here. client_credentials
        // principals already carry their baked 'perm' claims from OpenIdTokenManager.Issue (that
        // grant is in ProcessSignInClaimsHandler's ClientOnlyGrantTypes skip-set).
        return Microsoft.AspNetCore.Http.Results.SignIn(principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // Why: shared 403 shape for every "grant rejected" outcome (bad credentials, external token
    // validation failure, no/ambiguous external identity provider) — one Forbid construction, not
    // duplicated per failure branch.
    private static IResult ForbidInvalidGrant()
        => Microsoft.AspNetCore.Http.Results.Forbid(
            new AuthenticationProperties(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The credentials provided are invalid.",
            }),
            new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });

    // ── refresh_token / authorization_code ────────────────────────────────────────

    private async Task<IResult> HandleCodeOrRefresh()
    {
        // Why: For refresh and auth-code, authenticate against the current scheme to retrieve
        // the stored principal (already validated by OpenIddict's built-in handlers).
        // Re-sign in with that principal so ProcessSignInClaimsHandler re-resolves FDW
        // perms — ensures permission changes are reflected on each refresh.
        var request = HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("The OpenIddict server request cannot be retrieved.");

        var authResult = await HttpContext.AuthenticateAsync(
            OpenIddictServerAspNetCoreDefaults.AuthenticationScheme).ConfigureAwait(false);

        if (authResult.Principal is null)
        {
            return Microsoft.AspNetCore.Http.Results.Forbid(
                new AuthenticationProperties(new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = OpenIddictConstants.Errors.InvalidGrant,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The token is no longer valid.",
                }),
                new[] { OpenIddictServerAspNetCoreDefaults.AuthenticationScheme });
        }

        // Why: Overwrite tenant/org/cross-tenant claims BEFORE SignIn so that
        // ProcessSignInClaimsHandler picks up the new tenant context and re-resolves
        // perms for the requested tenant. This is how refresh-based tenant switching works.
        OverwriteTenantClaims(authResult.Principal, request);

        return Microsoft.AspNetCore.Http.Results.SignIn(authResult.Principal, authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────────

    // Why external_identity is capability-gated (FDW-624): when no external-identity domain is
    // registered the resolver is absent, so this deployment genuinely does not support that grant.
    // Reporting it as unsupported_grant_type (the fall-through below) is the honest answer — the
    // alternative, accepting the grant and failing later, would misrepresent what the server can do.
    // password / agent_key are unconditional; they do not depend on the external-identity capability.
    private bool IsCustomGrant(OpenIddictRequest request)
        => string.Equals(request.GrantType, "password", StringComparison.OrdinalIgnoreCase)
           || string.Equals(request.GrantType, "agent_key", StringComparison.OrdinalIgnoreCase)
           || (string.Equals(request.GrantType, "external_identity", StringComparison.OrdinalIgnoreCase)
               && _externalIdentityProviderResolver is not null);

    // Why: client_credentials carries the client_id/client_secret as Subject/Credential — the SAME
    // two fields every other grant uses for "identity being asserted" / "proof" — so OpenIdTokenManager
    // resolves and verifies them without a separate request shape. The external_identity branch is
    // FDW-owned (below): it resolves + validates the external token here, BEFORE the request ever
    // reaches OpenIdTokenManager, so IssueForExternalIdentity only ever sees an already-validated
    // ExternalPrincipal.
    private async Task<IGenericResult<TokenIssuanceRequest>> MapToIssuanceRequest(OpenIddictRequest request, CancellationToken cancellationToken)
    {
        // Why: tenant and org are custom parameters passed as form fields alongside the grant.
        // GetParameter returns OpenIddictParameter; cast to string? for null-safe string extraction.
        var tenantParam = (string?)request.GetParameter("tenant");
        var orgParam = (string?)request.GetParameter("org");
        var crossTenantParam = (string?)request.GetParameter("cross_tenant");

        Guid? tenantId = Guid.TryParse(tenantParam, out var t) ? t : null;
        Guid? orgId = Guid.TryParse(orgParam, out var o) ? o : null;
        var isCrossTenant = string.Equals(crossTenantParam, "true", StringComparison.OrdinalIgnoreCase);

        var issuanceRequest = new TokenIssuanceRequest
        {
            GrantType = request.GrantType ?? string.Empty,
            Scopes = request.GetScopes().ToList(),
            TenantId = tenantId,
            OrgId = orgId,
            IsCrossTenant = isCrossTenant,
        };

        if (string.Equals(request.GrantType, "external_identity", StringComparison.OrdinalIgnoreCase))
            return await MapExternalIdentityGrant(request, issuanceRequest, cancellationToken).ConfigureAwait(false);

        var isClientCredentials = request.IsClientCredentialsGrantType();
        issuanceRequest.Subject = isClientCredentials ? request.ClientId : request.Username;
        issuanceRequest.Credential = isClientCredentials ? request.ClientSecret : request.Password;

        return GenericResult<TokenIssuanceRequest>.Success(issuanceRequest);
    }

    // Why: reads the subject_token (the external IdP's token) and provider (the ExternalIdentityProviderTypes
    // config name to validate against) form params, resolves the IExternalIdentityProvider — by the
    // provider param when present, else the sole active configuration, else fails loud (never guesses
    // among multiple) — validates the token, and sets the resulting ClaimsPrincipal on the request.
    private async Task<IGenericResult<TokenIssuanceRequest>> MapExternalIdentityGrant(
        OpenIddictRequest request, TokenIssuanceRequest issuanceRequest, CancellationToken cancellationToken)
    {
        var subjectToken = (string?)request.GetParameter("subject_token");
        var providerName = (string?)request.GetParameter("provider");

        if (string.IsNullOrEmpty(subjectToken))
            return GenericResult<TokenIssuanceRequest>.Failure(
                OpenIddictProviderLog.IssuanceMissingSubjectOrCredential(_logger, request.GrantType ?? "external_identity"));

        // Why: IsCustomGrant already gates this grant on the resolver being present, so reaching here
        // with none means the two disagree — report it loudly rather than dereference. No fallback:
        // there is nothing to guess at, the capability is simply absent.
        if (_externalIdentityProviderResolver is null)
            return GenericResult<TokenIssuanceRequest>.Failure(
                OpenIddictProviderLog.ExternalIdentityLookupFailed(_logger, providerName ?? "(none)",
                    "no external-identity domain is registered, so the external_identity grant is not supported by this deployment."));

        var providerResult = await _externalIdentityProviderResolver.Resolve(providerName, cancellationToken).ConfigureAwait(false);
        if (!providerResult.IsSuccess)
            return providerResult.ToNewResult<TokenIssuanceRequest>();

        var validateResult = await providerResult.Value!.ValidateExternalToken(subjectToken, cancellationToken).ConfigureAwait(false);
        if (!validateResult.IsSuccess)
            return validateResult.ToNewResult<TokenIssuanceRequest>();

        issuanceRequest.ExternalPrincipal = validateResult.Value;
        return GenericResult<TokenIssuanceRequest>.Success(issuanceRequest);
    }

    // ── refresh_token / authorization_code — tenant switching ─────────────────────

    private static void OverwriteTenantClaims(System.Security.Claims.ClaimsPrincipal principal, OpenIddictRequest request)
    {
        var tenantParam = (string?)request.GetParameter("tenant");
        var orgParam = (string?)request.GetParameter("org");
        var crossTenantParam = (string?)request.GetParameter("cross_tenant");

        // Why: On refresh, the caller may request a tenant switch (tenant= param).
        // We overwrite the claims on the stored principal so ProcessSignInClaimsHandler
        // re-resolves perms for the new tenant on the next sign-in pipeline pass.
        if (principal.Identity is not System.Security.Claims.ClaimsIdentity identity)
            return;

        // Remove existing tenant/org/cross-tenant claims before setting new ones.
        foreach (var existing in identity.FindAll(ClaimDefinitions.tenantId.Name).ToList())
            identity.RemoveClaim(existing);
        foreach (var existing in identity.FindAll(ClaimDefinitions.orgId.Name).ToList())
            identity.RemoveClaim(existing);
        foreach (var existing in identity.FindAll(ClaimDefinitions.crossTenant.Name).ToList())
            identity.RemoveClaim(existing);

        var isCrossTenant = string.Equals(crossTenantParam, "true", StringComparison.OrdinalIgnoreCase);
        if (isCrossTenant)
        {
            identity.AddClaim(new System.Security.Claims.Claim(ClaimDefinitions.crossTenant.Name, "true"));
        }
        else if (Guid.TryParse(tenantParam, out var tenantId))
        {
            identity.AddClaim(new System.Security.Claims.Claim(ClaimDefinitions.tenantId.Name, tenantId.ToString()));
            if (Guid.TryParse(orgParam, out var orgId))
                identity.AddClaim(new System.Security.Claims.Claim(ClaimDefinitions.orgId.Name, orgId.ToString()));
        }
        // Why: When no tenant/cross_tenant param is present on refresh, the existing
        // tenant_id claim on the stored principal is preserved — the user stays in the
        // same tenant across refresh cycles unless they explicitly switch.
    }
}
