using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;

namespace ReferenceTransformations.Endpoints;

/// <summary>
/// Soft-deletes a transform step from a field mapping's transform chain.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class DeleteFieldMappingTransformEndpoint : DeleteFieldMappingTransformEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="DeleteFieldMappingTransformEndpoint"/>.
    /// </summary>
    public DeleteFieldMappingTransformEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider)
        : base(dataGateway, dataSetProvider)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
