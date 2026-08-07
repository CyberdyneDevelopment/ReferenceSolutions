using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Pipelines.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to get pipeline execution history for a specific pipeline.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetPipelineHistoryEndpoint : GetPipelineHistoryEndpointBase
{
    /// <inheritdoc />
    public GetPipelineHistoryEndpoint(IDataGateway dataGateway) : base(dataGateway)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
