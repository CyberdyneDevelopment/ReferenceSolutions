using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataSets.Endpoints;

/// <summary>Counts the data sets by status (FDW-4488).</summary>
[ExcludeFromCodeCoverage]
public sealed class ListDataSetStatusCountsEndpoint : ListDataSetStatusCountsEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListDataSetStatusCountsEndpoint"/> class.</summary>
    public ListDataSetStatusCountsEndpoint(IDataSetStatuses statuses, ILogger<ListDataSetStatusCountsEndpoint> logger)
        : base(logger, statuses)
    {
    }
}
