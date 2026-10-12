using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to set the datum type of one or more fields of a DataSet (FDW-988).
/// </summary>
[ExcludeFromCodeCoverage]
public class SetDataSetFieldDatumTypesEndpoint : SetDataSetFieldDatumTypesEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetDataSetFieldDatumTypesEndpoint"/> class.
    /// </summary>
    public SetDataSetFieldDatumTypesEndpoint(DataSetConfigurationProvider dataSetProvider, ILogger<SetDataSetFieldDatumTypesEndpoint> logger)
        : base(logger, dataSetProvider)
    {
    }

    /// <inheritdoc />
    protected override string EndpointSummary => "Set data set field datum types";

    /// <inheritdoc />
    protected override string EndpointDescription => "Sets the datum type of one or more fields of a data set in one save; an id the data set lacks refuses the request.";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
