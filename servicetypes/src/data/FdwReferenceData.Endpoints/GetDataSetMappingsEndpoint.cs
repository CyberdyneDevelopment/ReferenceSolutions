using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;
using Fdw.Schema.Endpoints;

namespace FdwReferenceFieldMappings.Endpoints;

/// <summary>
/// Concrete endpoint to get field mappings for a dataset.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataSetMappingsEndpoint : GetDataSetMappingsEndpointBase
{
    /// <inheritdoc />
    public GetDataSetMappingsEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        ILogger<Fdw.Schema.Endpoints.GetDataSetMappingsEndpointBase> logger)
        : base(dataSetProvider, logger)
    {
    }
}
