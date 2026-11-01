using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Lists the stand-in strategies a data set field can be given.</summary>
[ExcludeFromCodeCoverage]
public sealed class ListStandInStrategiesEndpoint : ListStandInStrategiesEndpointBase
{
    /// <summary>Initializes a new instance of <see cref="ListStandInStrategiesEndpoint"/>.</summary>
    public ListStandInStrategiesEndpoint(ILogger<ListStandInStrategiesEndpoint> logger)
        : base(logger)
    {
    }
}
