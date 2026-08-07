using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to list all pipeline execution history with pagination.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListPipelineHistoryEndpoint : ListPipelineHistoryEndpointBase
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
