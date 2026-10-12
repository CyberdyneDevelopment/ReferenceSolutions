using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Endpoint to delete a data view: <c>DELETE /data-views/{Name}</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteDataViewEndpoint : DeleteDataViewEndpointBase
{
    /// <inheritdoc />
    public DeleteDataViewEndpoint(
        ILogger<DeleteDataViewEndpointBase> logger,
        IDataSetVariationConfigurationProvider variations,
        IAuthenticationContextAccessor authContext)
        : base(logger, variations, authContext)
    {
    }
}
