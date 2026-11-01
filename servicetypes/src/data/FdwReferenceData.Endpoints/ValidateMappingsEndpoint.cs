using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data;
using Microsoft.Extensions.Logging;
using Fdw.Schema.Endpoints;

namespace FdwReferenceFieldMappings.Endpoints;

/// <summary>
/// Concrete endpoint to validate field mappings.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ValidateMappingsEndpoint : ValidateMappingsEndpointBase
{
    /// <inheritdoc />
    public ValidateMappingsEndpoint(
        DataSetConfigurationProvider dataSetProvider,
        ILogger<Fdw.Schema.Endpoints.ValidateMappingsEndpointBase> logger)
        : base(dataSetProvider, logger)
    {
    }
}
