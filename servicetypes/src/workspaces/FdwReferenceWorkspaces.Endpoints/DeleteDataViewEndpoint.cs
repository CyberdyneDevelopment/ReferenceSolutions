using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Endpoint to delete a data view: <c>DELETE /data-views/{Name}</c>.</summary>
/// <remarks>The person who saved the view may delete it, and so may a holder of <c>data-views:delete</c> in the view's tenant (FDW-2328).</remarks>
[ExcludeFromCodeCoverage]
public sealed class DeleteDataViewEndpoint : DeleteDataViewEndpointBase
{
    private readonly IScopeAuthority _scopes;

    /// <inheritdoc />
    public DeleteDataViewEndpoint(
        ILogger<DeleteDataViewEndpointBase> logger,
        IDataSetVariationConfigurationProvider variations,
        IAuthenticationContextAccessor authContext,
        IScopeAuthority scopes)
        : base(logger, variations, authContext)
    {
        _scopes = scopes;
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> MayChange(IDataSetVariationImplementationConfiguration view, CancellationToken cancellationToken)
    {
        var saver = await base.MayChange(view, cancellationToken).ConfigureAwait(false);
        return saver.IsSuccess
            ? saver
            : await DataViewChangeRule.HoldsRight(_scopes, view, "data-views:delete", Logger, cancellationToken).ConfigureAwait(false);
    }
}
