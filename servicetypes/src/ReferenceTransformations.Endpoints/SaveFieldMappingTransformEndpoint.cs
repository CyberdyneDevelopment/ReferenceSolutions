using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;

namespace ReferenceTransformations.Endpoints;

/// <summary>
/// Creates or updates a transform step in a field mapping's transform chain.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SaveFieldMappingTransformEndpoint : SaveFieldMappingTransformEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="SaveFieldMappingTransformEndpoint"/>.
    /// </summary>
    public SaveFieldMappingTransformEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider)
        : base(dataGateway, dataSetProvider)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
