using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;

namespace ReferenceTransformations.Endpoints;

/// <summary>Changes a transform already in a field mapping's chain.</summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateFieldMappingTransformEndpoint : UpdateFieldMappingTransformEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="UpdateFieldMappingTransformEndpoint"/> class.</summary>
    /// <param name="dataGateway">The gateway used for all reads and writes.</param>
    /// <param name="dataSetProvider">Owns the configuration store's name and path.</param>
    public UpdateFieldMappingTransformEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider)
        : base(dataGateway, dataSetProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
