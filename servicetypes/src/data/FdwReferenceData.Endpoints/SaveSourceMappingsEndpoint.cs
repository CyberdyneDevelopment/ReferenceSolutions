using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Microsoft.Extensions.Logging;
using Fdw.Schema.Endpoints;

namespace FdwReferenceFieldMappings.Endpoints;

/// <summary>
/// Concrete endpoint to save source field mappings.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SaveSourceMappingsEndpoint : SaveSourceMappingsEndpointBase
{
    /// <inheritdoc />
    public SaveSourceMappingsEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        ILogger<Fdw.Schema.Endpoints.SaveSourceMappingsEndpointBase> logger)
        : base(dataSetProvider, logger)
    {
    }
}
