using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Endpoint to change a data view: <c>PATCH /data-views/{Name}</c>.</summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateDataViewEndpoint : UpdateDataViewEndpointBase
{
    /// <inheritdoc />
    public UpdateDataViewEndpoint(
        ILogger<UpdateDataViewEndpointBase> logger,
        IDataSetVariationConfigurationProvider variations,
        IDataSetConfigurationProvider dataSets,
        IAuthenticationContextAccessor authContext)
        : base(logger, variations, dataSets, authContext)
    {
    }
}
