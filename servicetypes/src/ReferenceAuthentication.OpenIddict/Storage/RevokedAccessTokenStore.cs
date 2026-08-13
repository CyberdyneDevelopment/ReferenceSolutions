using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using ReferenceAuthentication.OpenIddict.Logging;
using ReferenceAuthentication.OpenIddict.Storage.Models;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Microsoft.Extensions.Logging;
using Query = Fdw.Commands.Data.DataQuery;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage;

/// <summary>
/// DataGateway-backed domain service for <c>auth.RevokedAccessToken</c> — the invalidation-list
/// backing <see cref="OpenIdTokenManager.Invalidate"/>/<see cref="OpenIdTokenManager.Validate"/>.
/// Access tokens are stateless RS256 JWTs (<c>DisableTokenStorage</c> for access tokens), so
/// revocation is tracked as a deny-list keyed on the token's <c>jti</c> claim rather than by
/// updating a persisted token row.
/// </summary>
/// <remarks>
/// Inherits <see cref="OpenIddictStoreBase"/> for its shared DataGateway query helpers, exactly like
/// <see cref="ExternalIdentityService"/> — reused infrastructure, not a public store contract.
/// </remarks>
internal sealed class RevokedAccessTokenStore : OpenIddictStoreBase
{
    private const string RevokedAccessTokenContainer = "RevokedAccessToken";

    public RevokedAccessTokenStore(Lazy<IDataGateway> dataGateway, IAuthenticationContextAccessor authContext, ILogger<RevokedAccessTokenStore>? logger)
        : base(dataGateway, authContext, logger)
    {
    }

    /// <summary>
    /// Inserts a revocation row for <paramref name="jti"/> so subsequent <see cref="IsRevoked"/>
    /// checks reject it until <paramref name="expiresAt"/>.
    /// </summary>
    public async Task<IGenericResult> Invalidate(Guid jti, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        OpenIddictStoreLog.RevokedTokenInsertStarted(Logger, jti);

        var record = new RevokedAccessTokenRecord
        {
            Jti = jti,
            RevokedAt = DateTimeOffset.UtcNow,
            ExpiresAt = expiresAt,
        };

        var insertResult = await InsertVersion(RevokedAccessTokenContainer, record, cancellationToken).ConfigureAwait(false);
        if (!insertResult.IsSuccess)
            return GenericResult.Failure(
                OpenIddictStoreLog.RevokedTokenInsertFailed(
                    Logger, new InvalidOperationException(insertResult.CurrentMessage), jti));

        OpenIddictStoreLog.RevokedTokenInserted(Logger, jti);
        return GenericResult.Success();
    }

    /// <summary>
    /// Returns whether <paramref name="jti"/> has an unexpired revocation row — i.e. the token must
    /// be rejected by <see cref="OpenIdTokenManager.Validate"/> even though its signature/expiry are
    /// otherwise valid.
    /// </summary>
    public async Task<IGenericResult<bool>> IsRevoked(Guid jti, CancellationToken cancellationToken)
    {
        OpenIddictStoreLog.RevokedTokenCheckStarted(Logger, jti);

        var command = Query.From<RevokedAccessTokenRecord>(DataStoreName, PathName, RevokedAccessTokenContainer)
            .Where(r => r.Jti).Equal(jti)
            .Where(r => r.ExpiresAt).GreaterThan(DateTimeOffset.UtcNow)
            .Build();

        var result = await QueryCurrent<RevokedAccessTokenRecord>(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return GenericResult<bool>.Failure(
                OpenIddictStoreLog.RevokedTokenCheckFailed(
                    Logger, new InvalidOperationException(result.CurrentMessage), jti));

        return GenericResult<bool>.Success(result.Value!.Any());
    }
}
