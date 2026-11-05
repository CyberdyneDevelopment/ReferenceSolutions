using System.Threading;
using System.Threading.Tasks;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Authorization.Abstractions;
using Fdw.Services.Workspaces.Results;
using Microsoft.Extensions.Logging;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>
/// The reference API's policy for who may change or delete a data view besides the person who saved it (FDW-2328): a holder of the
/// right at the view's scope or above.
/// </summary>
/// <remarks>
/// This shows how a developer uses the framework's question (<see cref="IScopeAuthority"/>); it is one answer, not the framework's.
/// A view sits in its tenant (a data view carries no organization), so the scope is the view's tenant and the right is the one the
/// route is guarded by: <c>data-views:write</c> to change, <c>data-views:delete</c> to delete. A developer who wants another policy
/// overrides <c>MayChange</c> on the endpoint with their own.
/// </remarks>
internal static class DataViewChangeRule
{
    /// <summary>Says whether the caller holds a right at the scope of a view.</summary>
    /// <param name="scopes">The framework's scope question.</param>
    /// <param name="view">The view a change is asked of.</param>
    /// <param name="right">The right asked for.</param>
    /// <param name="logger">The logger a refusal is written to.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>Success when the caller holds the right at the view's tenant; otherwise the refusal, which names the right and the scope.</returns>
    internal static Task<IGenericResult> HoldsRight(
        IScopeAuthority scopes, IDataSetVariationImplementationConfiguration view, string right, ILogger logger, CancellationToken cancellationToken)
        => view.TenantId is { } tenantId
            ? scopes.May(right, new ThingScope(tenantId, null), cancellationToken)
            : Task.FromResult<IGenericResult>(GenericResult.Failure(
                WorkspacesResultCodes.ByName("DataViewWriteNotPermitted"), logger,
                ResultDetails.Create("name", view.Name, "reason", "the view names no tenant, so there is no scope to judge it at")));
}
