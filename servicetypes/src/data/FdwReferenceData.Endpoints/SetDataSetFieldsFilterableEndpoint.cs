using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to choose whether API callers may filter on one or more fields of a DataSet.
/// </summary>
[ExcludeFromCodeCoverage]
public class SetDataSetFieldsFilterableEndpoint : SetDataSetFieldsFilterableEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SetDataSetFieldsFilterableEndpoint"/> class.
    /// </summary>
    public SetDataSetFieldsFilterableEndpoint(DataSetConfigurationProvider dataSetProvider, ILogger<SetDataSetFieldsFilterableEndpoint> logger)
        : base(logger, dataSetProvider)
    {
    }

    /// <inheritdoc />
    protected override string EndpointSummary => "Set data set fields filterable";

    /// <inheritdoc />
    protected override string EndpointDescription => "Chooses whether API callers may filter on one or more fields of a data set, in one save; an id the data set lacks refuses the request.";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
