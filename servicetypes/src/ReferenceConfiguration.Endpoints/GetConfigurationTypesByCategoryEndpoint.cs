using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;

namespace ReferenceConfiguration.Endpoints;

/// <summary>
/// Concrete endpoint to list configuration types by category.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetConfigurationTypesByCategoryEndpoint : Fdw.Operations.Endpoints.ConfigurationMetadata.GetConfigurationTypesByCategoryEndpoint
{
    /// <inheritdoc />
    public GetConfigurationTypesByCategoryEndpoint(IConfigurationContainerLookup containerLookup)
        : base(containerLookup)
    {
    }
}
