using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Workspaces.Endpoints;

namespace FdwReferenceWorkspaces.Endpoints;

/// <summary>Concrete endpoint to read one data view, whole: <c>POST /data-views/get</c>.</summary>
/// <remarks>
/// Behaviour lives entirely in the base. This exists so the endpoint has a declared type for its
/// <c>[TypeOption]</c> to name — auto-discovery is off, so a base with no declared concrete
/// compiles and returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class GetDataViewEndpoint : GetDataViewEndpointBase
{
    /// <inheritdoc />
    public GetDataViewEndpoint(
        ILogger<GetDataViewEndpointBase> logger,
        IDataSetVariationConfigurationProvider variations)
        : base(logger, variations)
    {
    }
}
