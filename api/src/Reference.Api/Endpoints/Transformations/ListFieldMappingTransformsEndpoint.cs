using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;

namespace Reference.Api.Endpoints.Transformations;

/// <summary>
/// Lists all transforms configured for a field mapping.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListFieldMappingTransformsEndpoint : ListFieldMappingTransformsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ListFieldMappingTransformsEndpoint"/>.
    /// </summary>
    public ListFieldMappingTransformsEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider)
        : base(dataGateway, dataSetProvider)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
