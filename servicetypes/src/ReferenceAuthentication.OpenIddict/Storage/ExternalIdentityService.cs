using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using ReferenceAuthentication.OpenIddict.Logging;
using ReferenceAuthentication.OpenIddict.Storage.Models;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Query = Fdw.Commands.Data.DataQuery;
using ReferenceAuthentication.OpenIddict;
using Fdw.Services.Authentication.OpenIddict;
using Fdw.Services.Authentication;
using Fdw.Services;
using Fdw;

namespace ReferenceAuthentication.OpenIddict.Storage;

/// <summary>
/// DataGateway-backed domain service for <c>auth.ExternalIdentity</c> — maps an external provider
/// subject to a FDW user GUID. Used exclusively by the external-identity issuance path.
/// </summary>
/// <remarks>
/// Inherits <see cref="OpenIddictStoreBase"/> for its shared DataGateway query helpers; the base
/// is reused infrastructure, not a public store contract. This type exposes the verb <c>FindUserId</c>.
/// </remarks>
internal sealed class ExternalIdentityService : OpenIddictStoreBase
{
    private const string ExternalIdentityContainer = "ExternalIdentity";

    public ExternalIdentityService(Lazy<IDataGateway> dataGateway, ILogger<ExternalIdentityService>? logger)
        : base(dataGateway, logger)
    {
    }

    /// <summary>
    /// Looks up the FDW UserId for the given external (provider, externalSubject) pair.
    /// Returns <see langword="null"/> value on success if no active mapping exists (not an error).
    /// Returns non-success only on DataGateway query failure.
    /// </summary>
    public async Task<IGenericResult<Guid?>> FindUserId(
        string provider,
        string externalSubject,
        CancellationToken cancellationToken)
    {
        OpenIddictProviderLog.ExternalIdentityLookupStarted(Logger, provider, externalSubject);

        var command = Query.From<ExternalIdentityRecord>(DataStoreName, PathName, ExternalIdentityContainer)
            .Where(r => r.Provider).Equal(provider)
            .Where(r => r.ExternalSubject).Equal(externalSubject)
            .Where(r => r.IsActive).Equal(true)
            .Build();

        var result = await QueryCurrent<ExternalIdentityRecord>(command, cancellationToken).ConfigureAwait(false);
        if (!result.IsSuccess)
            return result.ToNewResult<Guid?>();

        var row = result.Value!.FirstOrDefault();
        return GenericResult<Guid?>.Success(row is null ? null : (Guid?)row.UserId);
    }
}
