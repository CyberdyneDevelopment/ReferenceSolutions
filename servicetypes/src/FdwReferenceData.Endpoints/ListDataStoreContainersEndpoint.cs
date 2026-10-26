using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Data.Endpoints;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to list the containers of all data stores, one page at a time (flat list).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListDataStoreContainersEndpoint : ListDataStoreContainersEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListDataStoreContainersEndpoint"/> class.
    /// </summary>
    public ListDataStoreContainersEndpoint(IDataStoreContainerPages pages, ILogger<ListDataStoreContainersEndpoint> logger)
        : base(logger, pages) {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
