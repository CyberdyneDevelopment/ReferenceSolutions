using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to create a data view.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class CreateDataViewEndpoint : CreateDataViewEndpointBase
{
    /// <inheritdoc />
    public CreateDataViewEndpoint(
        ILogger<CreateDataViewEndpointBase> logger,
        IDataSetVariationConfigurationProvider variations,
        IDataSetConfigurationProvider dataSets,
        IAuthenticationContextAccessor authContext)
        : base(logger, variations, dataSets, authContext)
    {
    }
}
