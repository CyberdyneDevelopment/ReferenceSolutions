using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to get status of all pipelines in a single call.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class BulkPipelineStatusEndpoint : BulkPipelineStatusEndpointBase
{
    /// <inheritdoc />
    public BulkPipelineStatusEndpoint(IDataGateway dataGateway) : base(dataGateway)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
