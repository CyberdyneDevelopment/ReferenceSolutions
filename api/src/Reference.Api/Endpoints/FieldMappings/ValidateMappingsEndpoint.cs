using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to validate field mappings.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ValidateMappingsEndpoint : Fdw.Schema.Endpoints.ValidateMappingsEndpoint
{
    /// <inheritdoc />
    public ValidateMappingsEndpoint(
        IDataGateway dataGateway,
        ILogger<Fdw.Schema.Endpoints.ValidateMappingsEndpoint> logger)
        : base(dataGateway, logger)
    {
    }
}
