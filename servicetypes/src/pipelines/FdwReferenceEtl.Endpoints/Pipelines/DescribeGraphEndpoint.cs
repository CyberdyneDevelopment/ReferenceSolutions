using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Data.Transformations.Configuration;
using Fdw.Services.Pipelines.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferencePipelines.Endpoints;

/// <summary>
/// Endpoint to describe a graph pipeline draft: the fields at every node.
/// Sealed closure of the base class from Fdw.Services.Pipelines.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class DescribeGraphEndpoint : DescribeGraphEndpointBase
{
    /// <inheritdoc />
    public DescribeGraphEndpoint(
        IDataSetConfigurationProvider dataSets, ITransformationConfigurationProvider transformations, ILogger<DescribeGraphEndpoint> logger)
        : base(logger, dataSets, transformations)
    {
    }

    /// <inheritdoc />
    public override void Configure()
    {
        base.Configure();
        Tags("Pipelines");
    }
}
