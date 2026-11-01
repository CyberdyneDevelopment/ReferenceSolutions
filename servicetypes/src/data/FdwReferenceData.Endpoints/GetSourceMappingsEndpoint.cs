using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Schema.Endpoints;

namespace FdwReferenceFieldMappings.Endpoints;

/// <summary>
/// Concrete endpoint to get source field mappings.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetSourceMappingsEndpoint : GetSourceMappingsEndpointBase
{
    /// <inheritdoc />
    public GetSourceMappingsEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        ILogger<Fdw.Schema.Endpoints.GetSourceMappingsEndpointBase> logger)
        : base(dataSetProvider, logger)
    {
    }
}
