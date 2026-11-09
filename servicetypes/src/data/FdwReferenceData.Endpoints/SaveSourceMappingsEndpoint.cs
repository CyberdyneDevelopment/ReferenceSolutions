using System.Diagnostics.CodeAnalysis;
using Fdw.Data.DataSets.Abstractions;
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
        IDataSetConfigurationProvider dataSetProvider,
        ILogger<Fdw.Schema.Endpoints.SaveSourceMappingsEndpointBase> logger)
        : base(dataSetProvider, logger)
    {
    }
}
