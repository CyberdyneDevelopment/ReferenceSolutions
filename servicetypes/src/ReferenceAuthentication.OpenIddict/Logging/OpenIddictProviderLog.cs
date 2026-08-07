using System;
using Fdw.MessageLogging;
using Fdw.Messages;
using Microsoft.Extensions.Logging;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Logging;

/// <summary>
/// MessageLogging for the OpenIddict provider engine (signing keys, claims, issuance, validation).
/// EventId range: 7321–7424. The principal/tenant-resolution entries (7330–7335, 7400–7403,
/// 7408–7414) were relocated to <c>PrincipalResolverLog</c> in the core Authentication package
/// when <c>DefaultPrincipalResolver</c> moved there; those ids are retired from this class.
/// </summary>
[MessageLoggingTypeCode("OPENIDDICT")]
internal static partial class OpenIddictProviderLog
{
    // ── Signing key ─────────────────────────────────────────────────────────────────

    [MessageLogging(EventId = 11000, Level = LogLevel.Trace,
        Message = "Loading RS256 signing key from secret manager '{managerName}' key '{keyName}'.")]
    internal static partial IGenericMessage SigningKeyLoadStarted(ILogger logger, string managerName, string keyName);

    [MessageLogging(EventId = 11001, Level = LogLevel.Information,
        Message = "RS256 signing key loaded from secret manager '{managerName}' key '{keyName}'; keyId={keyId}.")]
    internal static partial IGenericMessage SigningKeyLoaded(ILogger logger, string managerName, string keyName, string keyId);

    // Why: Auth-cannot-function — at startup a missing signing key means EVERY token issuance fails.
    // Promoted Error→Critical so ops paging on Critical fires.
    [MessageLogging(EventId = 61000, Level = LogLevel.Critical,
        Message = "Signing key missing: secret manager '{managerName}' key '{keyName}' returned no value. RS256 signing unavailable.")]
    internal static partial IGenericMessage SigningKeyMissing(ILogger logger, string managerName, string keyName);

    [MessageLogging(EventId = 71000, Level = LogLevel.Error,
        Message = "Signing key load failed: secret manager '{managerName}' key '{keyName}'. {message}")]
    internal static partial IGenericMessage SigningKeyLoadFailed(ILogger logger, string managerName, string keyName, string message);

    [MessageLogging(EventId = 91000, Level = LogLevel.Error,
        Message = "Signing key PEM parse failed for key '{keyName}': {message}")]
    internal static partial IGenericMessage SigningKeyParseFailed(ILogger logger, Exception exception, string keyName, string message);

    // Why: Auth-cannot-function — without the secret manager the signing key never loads, so the
    // auth server cannot mint any token. Promoted Error→Critical.
    [MessageLogging(EventId = 61001, Level = LogLevel.Critical,
        Message = "Secret manager '{managerName}' not found. Cannot load RS256 signing key.")]
    internal static partial IGenericMessage SigningKeyManagerNotFound(ILogger logger, string managerName);

    // ── ProcessSignIn claims handler ────────────────────────────────────────────────

    [MessageLogging(EventId = 11002, Level = LogLevel.Trace,
        Message = "ProcessSignIn claims handler: baking FDW claims for subject='{subject}' grantType='{grantType}'.")]
    internal static partial IGenericMessage ProcessSignInStarted(ILogger logger, string subject, string grantType);

    [MessageLogging(EventId = 51000, Level = LogLevel.Error,
        Message = "ProcessSignIn claims handler: subject claim missing from principal. Cannot bake FDW claims.")]
    internal static partial IGenericMessage ProcessSignInSubjectMissing(ILogger logger);

    [MessageLogging(EventId = 91001, Level = LogLevel.Error,
        Message = "ProcessSignIn claims handler: FDW claim baking failed for subject='{subject}'. {message}")]
    internal static partial IGenericMessage ProcessSignInClaimBakeFailed(ILogger logger, string subject, string message);

    // ── Issuance service ────────────────────────────────────────────────────────────

    [MessageLogging(EventId = 11003, Level = LogLevel.Trace,
        Message = "Token issuance started: grantType='{grantType}' subject='{subject}'.")]
    internal static partial IGenericMessage IssuanceStarted(ILogger logger, string grantType, string? subject);

    [MessageLogging(EventId = 21000, Level = LogLevel.Error,
        Message = "Token issuance failed: grantType='{grantType}' — subject or credential is missing from request.")]
    internal static partial IGenericMessage IssuanceMissingSubjectOrCredential(ILogger logger, string grantType);

    [MessageLogging(EventId = 11004, Level = LogLevel.Information,
        Message = "Token issued: grantType='{grantType}' subject='{subject}' expiresAt={expiresAt}.")]
    internal static partial IGenericMessage TokenIssued(ILogger logger, string grantType, string subject, DateTimeOffset expiresAt);

    [MessageLogging(EventId = 91002, Level = LogLevel.Error,
        Message = "Token issuance failed: grantType='{grantType}' subject='{subject}'. {message}")]
    internal static partial IGenericMessage IssuanceFailed(ILogger logger, string grantType, string subject, string message);

    // Why: Expected user-driven denial (bad password), not a system fault. Demoted Error→Warning.
    [MessageLogging(EventId = 51001, Level = LogLevel.Warning,
        Message = "Invalid credentials for subject='{subject}' grantType='{grantType}'.")]
    internal static partial IGenericMessage InvalidCredentials(ILogger logger, string subject, string grantType);

    // Why: Expected user-driven denial (unknown/inactive user), not a system fault. Demoted Error→Warning.
    [MessageLogging(EventId = 51002, Level = LogLevel.Warning,
        Message = "User '{username}' not found or inactive during token issuance.")]
    internal static partial IGenericMessage UserNotFound(ILogger logger, string username);

    [MessageLogging(EventId = 21001, Level = LogLevel.Error,
        Message = "Unsupported grant type '{grantType}' for OpenIddict issuance service.")]
    internal static partial IGenericMessage UnsupportedGrantType(ILogger logger, string grantType);

    [MessageLogging(EventId = 11005, Level = LogLevel.Trace,
        Message = "Token refresh started for refreshToken (truncated for security).")]
    internal static partial IGenericMessage RefreshStarted(ILogger logger);

    // Why: Expected denial (token expired/rotated), not a system fault. Demoted Error→Warning.
    [MessageLogging(EventId = 51003, Level = LogLevel.Warning,
        Message = "Refresh token invalid or expired.")]
    internal static partial IGenericMessage RefreshTokenInvalid(ILogger logger);

    [MessageLogging(EventId = 11006, Level = LogLevel.Trace,
        Message = "Logout started for subjectId='{subjectId}'.")]
    internal static partial IGenericMessage LogoutStarted(ILogger logger, string subjectId);

    [MessageLogging(EventId = 11007, Level = LogLevel.Information,
        Message = "Logout complete: revoked {count} refresh token(s) for subjectId='{subjectId}'.")]
    internal static partial IGenericMessage LogoutComplete(ILogger logger, int count, string subjectId);

    [MessageLogging(EventId = 11008, Level = LogLevel.Trace,
        Message = "Revoke started.")]
    internal static partial IGenericMessage RevokeStarted(ILogger logger);

    // Why: Expected outcome (token already gone/never existed), not a system fault. Demoted Error→Warning.
    [MessageLogging(EventId = 31000, Level = LogLevel.Warning,
        Message = "Revoke failed: token not found.")]
    internal static partial IGenericMessage RevokeTokenNotFound(ILogger logger);

    // ── External identity ───────────────────────────────────────────────────────────

    [MessageLogging(EventId = 11009, Level = LogLevel.Trace,
        Message = "External identity lookup: provider='{provider}' externalSubject='{externalSubject}'.")]
    internal static partial IGenericMessage ExternalIdentityLookupStarted(ILogger logger, string provider, string externalSubject);

    // Why: Expected denial (no link row for this external identity), not a system fault. Demoted Error→Warning.
    [MessageLogging(EventId = 51004, Level = LogLevel.Warning,
        Message = "External identity not found: provider='{provider}' externalSubject='{externalSubject}'. No auth.ExternalIdentity link row exists.")]
    internal static partial IGenericMessage ExternalIdentityNotFound(ILogger logger, string provider, string externalSubject);

    [MessageLogging(EventId = 51005, Level = LogLevel.Error,
        Message = "External identity lookup failed: provider='{provider}'. {message}")]
    internal static partial IGenericMessage ExternalIdentityLookupFailed(ILogger logger, string provider, string message);

    // Why: IExternalIdentityProvisioner is opt-in and off by default — this fires only when a host has
    // registered one AND it successfully provisioned a new FDW user for a lookup miss.
    [MessageLogging(EventId = 11023, Level = LogLevel.Information,
        Message = "External identity provisioned: provider='{provider}' externalSubject='{externalSubject}' new userId={userId}.")]
    internal static partial IGenericMessage ExternalIdentityProvisioned(ILogger logger, string provider, string externalSubject, Guid userId);

    // Why: logged BEFORE calling ExternalIdentityProvisionerBindingConfigurationProvider.ResolveProvisionerName,
    // so a hung/slow binding lookup is visible in traces even before any result comes back.
    [MessageLogging(EventId = 11045, Level = LogLevel.Trace,
        Message = "Resolving provisioner binding for external_identity issuance: provider='{provider}' tenantId={tenantId}.")]
    internal static partial IGenericMessage ProvisionerBindingLookupStarted(ILogger logger, string provider, string tenantId);

    // Why: a binding resolved to a provisionerName, but that name doesn't resolve to a registered
    // IExternalIdentityProvisioner — a distinct, dependency-category failure from "no binding at all"
    // (ExternalIdentityNotFound) or "provisioner ran but declined" (ExternalIdentityProvisioningFailed).
    // Propagated (not masked) — a misconfigured binding is a hard error, never treated as not-found.
    [MessageLogging(EventId = 71022, Level = LogLevel.Error,
        Message = "Provisioner resolution failed for external_identity issuance: provisionerName='{provisionerName}' provider='{provider}'. {message}")]
    internal static partial IGenericMessage ProvisionerResolutionFailed(ILogger logger, string provisionerName, string provider, string message);

    // Why: the provisioner resolved and ran, but Provision() itself failed — distinct from
    // ExternalIdentityProvisioned (success) and from ProvisionerResolutionFailed (resolution, not
    // execution). Propagated to the caller unchanged; this call only adds issuance-layer context.
    [MessageLogging(EventId = 51011, Level = LogLevel.Error,
        Message = "External identity provisioning failed: provisionerName='{provisionerName}' provider='{provider}' externalSubject='{externalSubject}'. {message}")]
    internal static partial IGenericMessage ExternalIdentityProvisioningFailed(ILogger logger, string provisionerName, string provider, string externalSubject, string message);

    // ── Validation service ──────────────────────────────────────────────────────────

    [MessageLogging(EventId = 11010, Level = LogLevel.Trace,
        Message = "Token validation started.")]
    internal static partial IGenericMessage ValidationStarted(ILogger logger);

    [MessageLogging(EventId = 51006, Level = LogLevel.Warning,
        Message = "Token validation failed: {reason}")]
    internal static partial IGenericMessage ValidationFailed(ILogger logger, string reason);

    [MessageLogging(EventId = 11011, Level = LogLevel.Trace,
        Message = "Token validation succeeded: subject='{subject}' expiresAt={expiresAt}.")]
    internal static partial IGenericMessage ValidationSucceeded(ILogger logger, string subject, DateTimeOffset expiresAt);

    // Why: Auth-cannot-function — without the signing key EVERY token validation fails. Promoted Error→Critical.
    [MessageLogging(EventId = 61002, Level = LogLevel.Critical,
        Message = "Signing key not available during token validation. Validation cannot proceed.")]
    internal static partial IGenericMessage ValidationSigningKeyUnavailable(ILogger logger);

    // ── Outbound credential service ─────────────────────────────────────────────────

    [MessageLogging(EventId = 11012, Level = LogLevel.Trace,
        Message = "Acquiring outbound credential for clientId='{clientId}'.")]
    internal static partial IGenericMessage OutboundAcquireStarted(ILogger logger, string clientId);

    [MessageLogging(EventId = 11013, Level = LogLevel.Information,
        Message = "Outbound credential acquired from cache for clientId='{clientId}'.")]
    internal static partial IGenericMessage OutboundAcquiredFromCache(ILogger logger, string clientId);

    [MessageLogging(EventId = 51007, Level = LogLevel.Error,
        Message = "Outbound credential acquisition failed for clientId='{clientId}': {message}")]
    internal static partial IGenericMessage OutboundAcquireFailed(ILogger logger, string clientId, string message);

    // ── TypeOption / registration ────────────────────────────────────────────────────

    [MessageLogging(EventId = 11014, Level = LogLevel.Information,
        Message = "OpenIddict auth server provider registered: configName='{configName}' capabilities={capabilities}.")]
    internal static partial IGenericMessage ProviderRegistered(ILogger logger, string configName, string capabilities);

    // Why: Auth-cannot-function — an empty provider registry means the auth server cannot issue any
    // token, the same condition its twin SigningKeyStartupNoConfig already marks Critical. Promoted
    // Warning→Critical for consistency.
    [MessageLogging(EventId = 61003, Level = LogLevel.Critical,
        Message = "OpenIddict auth server: no enabled configurations found. Provider registry is empty.")]
    internal static partial IGenericMessage NoEnabledConfigurations(ILogger logger);

    [MessageLogging(EventId = 61004, Level = LogLevel.Error,
        Message = "OpenIddict auth server factory failed to create service for configName='{configName}': {message}")]
    internal static partial IGenericMessage FactoryCreateFailed(ILogger logger, string configName, string message);

    // Why: Auth-cannot-function — startup key load failed, so no token can be issued. Promoted Error→Critical.
    [MessageLogging(EventId = 61005, Level = LogLevel.Critical,
        Message = "OpenIddict signing key load failed at startup for configName='{configName}'. Auth server will not issue tokens until resolved.")]
    internal static partial IGenericMessage SigningKeyStartupFailed(ILogger logger, string configName);

    [MessageLogging(EventId = 11015, Level = LogLevel.Information,
        Message = "OpenIddict signing key loaded at startup for configName='{configName}'.")]
    internal static partial IGenericMessage SigningKeyStartupLoaded(ILogger logger, string configName);

    // ── Token endpoint ───────────────────────────────────────────────────────────────

    [MessageLogging(EventId = 11016, Level = LogLevel.Trace,
        Message = "Token endpoint: processing grant_type='{grantType}'.")]
    internal static partial IGenericMessage TokenEndpointStarted(ILogger logger, string grantType);

    [MessageLogging(EventId = 21002, Level = LogLevel.Warning,
        Message = "Token endpoint: unsupported grant_type='{grantType}' — rejected.")]
    internal static partial IGenericMessage TokenEndpointUnsupportedGrant(ILogger logger, string grantType);

    // Why: Expected user-driven denial (bad credentials at the token endpoint), not a system fault. Demoted Error→Warning.
    [MessageLogging(EventId = 51008, Level = LogLevel.Warning,
        Message = "Token endpoint: credential validation failed for grant_type='{grantType}' subject='{subject}'.")]
    internal static partial IGenericMessage TokenEndpointCredentialFailed(ILogger logger, string grantType, string? subject);

    // ── Forwarded-failure log entries (replaces ?? "fallback" pattern) ───────────────

    [MessageLogging(EventId = 91003, Level = LogLevel.Error,
        Message = "Upstream result carried no message. Context: {context}")]
    internal static partial IGenericMessage UpstreamResultNoMessage(ILogger logger, string context);

    [MessageLogging(EventId = 51009, Level = LogLevel.Error,
        Message = "Validation token format error: {reason}")]
    internal static partial IGenericMessage ValidationTokenFormatError(ILogger logger, string reason);

    // ── Tenant validation ────────────────────────────────────────────────────────

    [MessageLogging(EventId = 11017, Level = LogLevel.Information,
        Message = "Token issued with active tenantId={tenantId} for userId={userId}.")]
    internal static partial IGenericMessage TokenIssuedWithTenant(ILogger logger, string tenantId, string userId);

    // ── IAuthenticationService command dispatch ────────────────────────────────────

    [MessageLogging(EventId = 91004, Level = LogLevel.Error,
        Message = "Command dispatch type mismatch: expected '{expectedType}' but result is not assignable from '{actualType}'.")]
    internal static partial IGenericMessage CommandTypeMismatch(ILogger logger, string expectedType, string actualType);

    [MessageLogging(EventId = 91005, Level = LogLevel.Error,
        Message = "Command '{commandType}' is not dispatchable from IAuthenticationService.Execute — handled by OpenIddict pipeline directly.")]
    internal static partial IGenericMessage CommandNotDispatchableFromService(ILogger logger, string commandType);

    [MessageLogging(EventId = 91006, Level = LogLevel.Warning,
        Message = "Command '{commandType}' is not supported by OpenIddictAuthenticationService.")]
    internal static partial IGenericMessage CommandNotSupported(ILogger logger, string commandType);

    // ── ProcessSignIn / issuance decision-flow trace ──────────────────────────────────
    // Note: the principal-resolver decision traces (7408–7414) were relocated to
    // PrincipalResolverLog in the core Fdw.Services.Authentication package.

    /// <summary>Traces ProcessSignIn claim-bake detail (claims added, destinations).</summary>
    [MessageLogging(EventId = 11018, Level = LogLevel.Trace,
        Message = "ProcessSignIn bake: subject={subject} grantType={grantType} tenantId={tenantId} orgId={orgId} isCrossTenant={isCrossTenant} claimsAdded={claimsAdded}.")]
    internal static partial IGenericMessage ProcessSignInBakeTrace(ILogger logger, string subject, string grantType, string tenantId, string orgId, bool isCrossTenant, int claimsAdded);

    /// <summary>Traces issuance grant-branch and credential-verify outcome.</summary>
    [MessageLogging(EventId = 11019, Level = LogLevel.Trace,
        Message = "Issuance branch: grantType={grantType} subject={subject} verified={verified} requestedTenant={tenantId} isCrossTenant={isCrossTenant}.")]
    internal static partial IGenericMessage IssuanceBranchTrace(ILogger logger, string grantType, string subject, bool verified, string tenantId, bool isCrossTenant);

    // Why: Auth-cannot-function — no OpenIddict configuration exists in ConfigurationDb, so no
    // signing key can be loaded and the auth server cannot mint tokens. Critical (not Error) to
    // ensure ops paging fires. This is a configuration defect, not a transient failure.
    [MessageLogging(EventId = 61006, Level = LogLevel.Critical,
        Message = "No enabled OpenIddict configuration found in ConfigurationDb. Auth server cannot load signing key and will not issue tokens.")]
    internal static partial IGenericMessage SigningKeyStartupNoConfig(ILogger logger);

    [MessageLogging(EventId = 71001, Level = LogLevel.Error,
        Message = "OpenIddict configuration gateway load failed at startup: {message}")]
    internal static partial IGenericMessage SigningKeyStartupConfigLoadFailed(ILogger logger, string message);

    // ── client_credentials service-principal claim baking ─────────────────────────────

    [MessageLogging(EventId = 11020, Level = LogLevel.Trace,
        Message = "client_credentials: resolving effective permission set for service principal clientId='{clientId}'.")]
    internal static partial IGenericMessage ClientCredentialsClaimBakeStarted(ILogger logger, string clientId);

    [MessageLogging(EventId = 11021, Level = LogLevel.Information,
        Message = "client_credentials: baked {permCount} perm claim(s) into token for service principal clientId='{clientId}'.")]
    internal static partial IGenericMessage ClientCredentialsClaimBaked(ILogger logger, string clientId, int permCount);

    [MessageLogging(EventId = 51010, Level = LogLevel.Error,
        Message = "client_credentials: permission resolution failed for service principal clientId='{clientId}'. {message}")]
    internal static partial IGenericMessage ClientCredentialsClaimBakeFailed(ILogger logger, string clientId, string message);

    // ── Confidential client secret provisioning (startup) ─────────────────────────────

    [MessageLogging(EventId = 11022, Level = LogLevel.Information,
        Message = "OpenIddict client secret provisioned for confidential client '{clientId}'.")]
    internal static partial IGenericMessage ClientSecretProvisioned(ILogger logger, string clientId);

    // Why: a confidential client with no configured secret cannot complete client_credentials — but it
    // is a deployment-config gap (the secret env var isn't set for this environment), not a system fault.
    [MessageLogging(EventId = 61007, Level = LogLevel.Warning,
        Message = "OpenIddict confidential client '{clientId}' has no secret configured under key '{secretKey}'; skipped — client_credentials will fail until the secret is provisioned.")]
    internal static partial IGenericMessage ClientSecretNotConfigured(ILogger logger, string clientId, string secretKey);

    [MessageLogging(EventId = 61008, Level = LogLevel.Warning,
        Message = "OpenIddict client secret provisioning skipped: no OpenIddict server config or secret manager could be resolved.")]
    internal static partial IGenericMessage ClientSecretProvisionNoConfig(ILogger logger);
}
