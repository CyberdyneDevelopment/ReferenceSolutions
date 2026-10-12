using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to set the data classification of one or more fields of a DataSet.
/// </summary>
[ExcludeFromCodeCoverage]
public class ClassifyDataSetFieldsEndpoint : ClassifyDataSetFieldsEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClassifyDataSetFieldsEndpoint"/> class.
    /// </summary>
    public ClassifyDataSetFieldsEndpoint(DataSetConfigurationProvider dataSetProvider, ILogger<ClassifyDataSetFieldsEndpoint> logger)
        : base(logger, dataSetProvider)
    {
    }

    /// <inheritdoc />
    protected override string EndpointSummary => "Classify data set fields";

    /// <inheritdoc />
    protected override string EndpointDescription => "Sets the data classification of one or more fields of a data set in one save; an id the data set lacks refuses the request.";

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
