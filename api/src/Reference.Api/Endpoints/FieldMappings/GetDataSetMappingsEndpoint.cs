using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to get field mappings for a dataset.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetDataSetMappingsEndpoint : Fdw.Schema.Endpoints.GetDataSetMappingsEndpoint
{
    /// <inheritdoc />
    public GetDataSetMappingsEndpoint(
        IDataGateway dataGateway,
        ILogger<Fdw.Schema.Endpoints.GetDataSetMappingsEndpoint> logger)
        : base(dataGateway, logger)
    {
    }
}
