using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Endpoint to change a data view: <c>PATCH /data-views/{Name}</c>.</summary>
/// <remarks>The person who saved the view may change it, and so may a holder of <c>data-views:write</c> in the view's tenant (FDW-2328).</remarks>
[ExcludeFromCodeCoverage]
public sealed class UpdateDataViewEndpoint : UpdateDataViewEndpointBase
{
    private readonly IScopeAuthority _scopes;

    /// <inheritdoc />
    public UpdateDataViewEndpoint(
        ILogger<UpdateDataViewEndpointBase> logger,
        IDataSetVariationConfigurationProvider variations,
        IDataSetConfigurationProvider dataSets,
        IAuthenticationContextAccessor authContext,
        IScopeAuthority scopes,
        IFilterCalculations calculations)
        : base(logger, variations, dataSets, authContext, calculations)
    {
        _scopes = scopes;
    }

    /// <inheritdoc />
    protected override async Task<IGenericResult> MayChange(IDataViewDataSetVariationConfiguration view, CancellationToken cancellationToken)
    {
        var saver = await base.MayChange(view, cancellationToken).ConfigureAwait(false);
        return saver.IsSuccess
            ? saver
            : await DataViewChangeRule.HoldsRight(_scopes, view, "data-views:write", Logger, cancellationToken).ConfigureAwait(false);
    }
}
