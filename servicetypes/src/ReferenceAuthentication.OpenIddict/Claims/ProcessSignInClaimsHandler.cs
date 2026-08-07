using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Fdw.Services.Authentication.Abstractions;
using ReferenceAuthentication.OpenIddict.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.JsonWebTokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using static OpenIddict.Server.OpenIddictServerEvents;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Claims;

/// <summary>
/// OpenIddict event handler that bakes FDW role/permission claims into the access token
/// during every sign-in operation (authorization-code, client-credentials, and custom grants).
/// Both issuance paths — credential and external-identity — funnel through this baker.
/// FAIL-LOUD: missing subject or failed permission resolution → sign-in rejected.
/// </summary>
internal sealed class ProcessSignInClaimsHandler : IOpenIddictServerHandler<ProcessSignInContext>
{
    private readonly IPrincipalResolver _principalResolver;
    private readonly ILogger<ProcessSignInClaimsHandler> _logger;

    // Why: client_credentials tokens have no interactive subject; FDW user claims are not baked.
    private static readonly HashSet<string> ClientOnlyGrantTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        OpenIddictConstants.GrantTypes.ClientCredentials,
    };

    public ProcessSignInClaimsHandler(
        IPrincipalResolver principalResolver,
        ILogger<ProcessSignInClaimsHandler>? logger)
    {
        ArgumentNullException.ThrowIfNull(principalResolver);
        _principalResolver = principalResolver;
        _logger = logger ?? NullLogger<ProcessSignInClaimsHandler>.Instance;
    }

    /// <inheritdoc />
    public async ValueTask HandleAsync(ProcessSignInContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var grantType = context.Request?.GrantType ?? string.Empty;
        if (ClientOnlyGrantTypes.Contains(grantType))
            return;

        var subjectStr = ExtractSubject(context.Principal);
        if (subjectStr is null)
        {
            OpenIddictProviderLog.ProcessSignInSubjectMissing(_logger);
            context.Reject(OpenIddictConstants.Errors.InvalidToken,
                "FDW principal resolution failed: subject claim is missing.");
            return;
        }

        OpenIddictProviderLog.ProcessSignInStarted(_logger, subjectStr, grantType);

        if (!Guid.TryParse(subjectStr, out var userId))
        {
            OpenIddictProviderLog.ProcessSignInClaimBakeFailed(_logger, subjectStr, "Subject is not a valid GUID.");
            context.Reject(OpenIddictConstants.Errors.InvalidToken,
                "FDW principal resolution failed: subject is not a valid user identifier.");
            return;
        }

        // Why: principal is non-null here — ExtractSubject returned non-null only when principal is non-null.
        var principal = context.Principal!;
        var tenantId = TryParseGuid(principal.FindFirstValue(ClaimDefinitions.tenantId.Name));
        var orgId = TryParseGuid(principal.FindFirstValue(ClaimDefinitions.orgId.Name));
        var isCrossTenant = string.Equals(
            principal.FindFirstValue(ClaimDefinitions.crossTenant.Name), "true", StringComparison.OrdinalIgnoreCase);
        var extraRoles = CollectRoles(principal);

        await BakeClaims(context, userId, tenantId, orgId, isCrossTenant, extraRoles, subjectStr).ConfigureAwait(false);
    }

    private async ValueTask BakeClaims(
        ProcessSignInContext context,
        Guid userId,
        Guid? tenantId,
        Guid? orgId,
        bool isCrossTenant,
        IReadOnlyList<string> extraRoles,
        string subjectStr)
    {
        var resolveResult = await _principalResolver.Resolve(
            userId, tenantId, orgId, isCrossTenant, extraRoles, context.CancellationToken).ConfigureAwait(false);

        if (!resolveResult.IsSuccess)
        {
            OpenIddictProviderLog.ProcessSignInClaimBakeFailed(
                _logger, subjectStr, resolveResult.CurrentMessage!);
            // Why: tenant access denied and other auth-layer failures are invalid_grant (400), not
            // server_error (500). Infrastructure failures surface the same way — 400 is more
            // appropriate than leaking a 500 that suggests something internal exploded.
            context.Reject(OpenIddictConstants.Errors.InvalidGrant, "FDW claim resolution failed. Token not issued.");
            return;
        }

        var resolvedPrincipal = resolveResult.Value!;

        // Why: Merge FDW claims into the AccessTokenPrincipal — the principal the JWT writer
        // serializes from (its claims carry the AccessToken destination). The baking loop is
        // driven by ClaimDefinitions so downstream assemblies can add new claim types by declaring
        // a new [TypeOption] without changing FDW.
        var claimsAdded = 0;
        if (context.AccessTokenPrincipal?.Identity is System.Security.Claims.ClaimsIdentity atIdentity)
        {
            // Group resolved claims by type; skip sub (OpenIddict sets it).
            var groupedClaims = resolvedPrincipal.Claims
                .Where(c => !string.Equals(c.Type, ClaimDefinitions.sub.Name, StringComparison.Ordinal))
                .GroupBy(c => c.Type, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groupedClaims)
            {
                var claimType = group.Key;
                var values = group.Select(c => c.Value).ToList();

                var definition = ClaimDefinitions.ByName(claimType);

                if (definition != ClaimDefinitions.NotFound && definition.IsArray)
                {
                    // Why: array claims (e.g. roles) must ALWAYS serialize as a JSON array, even
                    // for a single value — clients cannot branch on string-vs-array. A plain Claim
                    // with one value serializes as a scalar; a Claim whose ValueType is
                    // JsonClaimValueTypes.JsonArray and whose value is a JSON array string is
                    // written verbatim as an array by the Microsoft.IdentityModel JWT writer.
                    var arrayClaim = new System.Security.Claims.Claim(
                        claimType,
                        JsonSerializer.Serialize(values),
                        JsonClaimValueTypes.JsonArray);
                    SetDestinations(arrayClaim, definition.Destinations);
                    atIdentity.AddClaim(arrayClaim);
                    claimsAdded++;
                }
                else
                {
                    // Scalar claim — emit each value individually.
                    // Why: for unknown extension claims default to AccessToken so downstream
                    // assemblies can add claims without an FDW change — this is a deliberate
                    // open-extension default, not a config fallback.
                    foreach (var value in values)
                    {
                        var scalarClaim = new System.Security.Claims.Claim(claimType, value);
                        if (definition != ClaimDefinitions.NotFound)
                            SetDestinations(scalarClaim, definition.Destinations);
                        else
                            scalarClaim.SetDestinations(OpenIddictConstants.Destinations.AccessToken);
                        atIdentity.AddClaim(scalarClaim);
                        claimsAdded++;
                    }
                }
            }
        }

        OpenIddictProviderLog.ProcessSignInBakeTrace(
            _logger, subjectStr, context.Request?.GrantType ?? string.Empty,
            tenantId?.ToString() ?? "(default)", orgId?.ToString() ?? "(none)", isCrossTenant, claimsAdded);

        context.Principal = resolvedPrincipal;
    }

    // Why: maps an FDW TokenDestinations string to the matching OpenIddict destination constant.
    // Both FDW and OpenIddict use the same wire values ("access_token"/"id_token") so the mapping
    // is a direct equality check rather than a switch on opaque symbols.
    private static string ToOpenIddictDestination(string fdwDestination)
        => string.Equals(fdwDestination, TokenDestinations.IdentityToken, StringComparison.Ordinal)
            ? OpenIddictConstants.Destinations.IdentityToken
            : OpenIddictConstants.Destinations.AccessToken;

    private static void SetDestinations(System.Security.Claims.Claim claim, System.Collections.Generic.IReadOnlyList<string> destinations)
    {
        var mapped = new string[destinations.Count];
        for (var i = 0; i < destinations.Count; i++)
            mapped[i] = ToOpenIddictDestination(destinations[i]);
        claim.SetDestinations(mapped);
    }

    private static string? ExtractSubject(ClaimsPrincipal? principal)
    {
        if (principal is null)
            return null;

        return principal.FindFirstValue(ClaimDefinitions.sub.Name)
            ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.Identity?.Name;
    }

    private static Guid? TryParseGuid(string? value)
        => Guid.TryParse(value, out var g) ? g : null;

    private static List<string> CollectRoles(ClaimsPrincipal principal)
    {
        var roles = new List<string>();
        foreach (var claim in principal.FindAll(ClaimDefinitions.roles.Name))
            roles.Add(claim.Value);
        return roles;
    }
}
