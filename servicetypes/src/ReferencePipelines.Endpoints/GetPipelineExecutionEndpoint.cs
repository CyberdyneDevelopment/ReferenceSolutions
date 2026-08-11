using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace ReferencePipelines.Endpoints;

/// <summary>
/// Concrete endpoint to get a specific pipeline execution record.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetPipelineExecutionEndpoint : GetPipelineExecutionEndpointBase
{
    /// <inheritdoc />
    public GetPipelineExecutionEndpoint(IDataGateway dataGateway) : base(dataGateway)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
