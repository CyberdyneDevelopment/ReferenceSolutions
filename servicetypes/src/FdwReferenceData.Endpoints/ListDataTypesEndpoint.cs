using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceDataStores.Endpoints;

/// <summary>
/// Endpoint to list the portable data types a container field's FrameworkDataType names (FDW-1103).
/// Sealed closure of the generic base class from Fdw.Services.Data.Endpoints.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListDataTypesEndpoint : ListDataTypesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListDataTypesEndpoint"/> class.</summary>
    public ListDataTypesEndpoint(ILogger<ListDataTypesEndpoint> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("DataTypes");
    }
}
