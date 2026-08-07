using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;

namespace Reference.Api.Endpoints.Transformations;

/// <summary>
/// Updates the ordinal positions of transforms in a field mapping's transform chain.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ReorderFieldMappingTransformsEndpoint : ReorderFieldMappingTransformsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of <see cref="ReorderFieldMappingTransformsEndpoint"/>.
    /// </summary>
    public ReorderFieldMappingTransformsEndpoint(IDataGateway dataGateway, DataSetConfigurationProvider dataSetProvider)
        : base(dataGateway, dataSetProvider)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("Transformations");
    }
}
