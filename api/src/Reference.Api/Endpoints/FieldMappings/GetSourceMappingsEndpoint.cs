using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to get source field mappings.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetSourceMappingsEndpoint : Fdw.Schema.Endpoints.GetSourceMappingsEndpoint
{
    /// <inheritdoc />
    public GetSourceMappingsEndpoint(
        IDataGateway dataGateway,
        ILogger<Fdw.Schema.Endpoints.GetSourceMappingsEndpoint> logger)
        : base(dataGateway, logger)
    {
    }
}
