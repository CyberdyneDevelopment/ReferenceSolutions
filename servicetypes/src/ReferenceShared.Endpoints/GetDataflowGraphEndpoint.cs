using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace ReferenceShared.Endpoints;

/// <summary>
/// Concrete endpoint to get the dataflow graph for a dataset.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataflowGraphEndpoint : Fdw.Operations.Endpoints.GetDataflowGraphEndpoint
{
    /// <inheritdoc />
    public GetDataflowGraphEndpoint(
        Fdw.Operations.Endpoints.DataflowGraphConfigurationProvider provider,
        ILogger<Fdw.Operations.Endpoints.GetDataflowGraphEndpoint> logger)
        : base(provider, logger)
    {
    }
}
