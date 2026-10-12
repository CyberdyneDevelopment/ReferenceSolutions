using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>
/// Endpoint to look up data sets by name, one page at a time, for pickers and name-taken checks (FDW-1194).
/// </summary>
[ExcludeFromCodeCoverage]
public class LookupDataSetsEndpoint : LookupDataSetsEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="LookupDataSetsEndpoint"/> class.</summary>
    public LookupDataSetsEndpoint(DataSetConfigurationProvider dataSetProvider, ILogger<LookupDataSetsEndpoint> logger)
        : base(logger, dataSetProvider)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataSets");
    }
}
