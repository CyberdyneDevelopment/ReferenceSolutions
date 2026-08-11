using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace ReferencePipelines.Endpoints;

/// <summary>
/// Concrete endpoint to list all pipeline execution history with pagination.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListPipelineHistoryEndpoint : ListPipelineHistoryEndpointBase
{
    /// <inheritdoc />
    public ListPipelineHistoryEndpoint(IDataGateway dataGateway) : base(dataGateway)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
