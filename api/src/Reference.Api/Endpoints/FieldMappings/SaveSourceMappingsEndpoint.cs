using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to save source field mappings.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SaveSourceMappingsEndpoint : Fdw.Schema.Endpoints.SaveSourceMappingsEndpoint
{
    /// <inheritdoc />
    public SaveSourceMappingsEndpoint(
        IDataGateway dataGateway,
        DataSetConfigurationProvider dataSetProvider,
        ILogger<Fdw.Schema.Endpoints.SaveSourceMappingsEndpoint> logger)
        : base(dataGateway, dataSetProvider, logger)
    {
    }
}
