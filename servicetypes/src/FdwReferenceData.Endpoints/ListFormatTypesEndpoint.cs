using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to list the formats a container can state, with their family, file extension and separator.
/// Sealed closure of the generic base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListFormatTypesEndpoint : ListFormatTypesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListFormatTypesEndpoint"/> class.</summary>
    public ListFormatTypesEndpoint(ILogger<ListFormatTypesEndpoint> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataStores");
    }
}
