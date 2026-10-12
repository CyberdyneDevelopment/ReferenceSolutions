using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to list the data classification levels (FDW-938).
/// Sealed closure of the generic base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListDataClassificationsEndpoint : ListDataClassificationsEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListDataClassificationsEndpoint"/> class.</summary>
    public ListDataClassificationsEndpoint(ILogger<ListDataClassificationsEndpoint> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataClassifications");
    }
}
