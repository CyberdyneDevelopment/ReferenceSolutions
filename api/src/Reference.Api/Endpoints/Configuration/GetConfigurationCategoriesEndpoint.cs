using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to list configuration categories.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetConfigurationCategoriesEndpoint : Fdw.Operations.Endpoints.ConfigurationMetadata.GetConfigurationCategoriesEndpoint
{
    /// <inheritdoc />
    public GetConfigurationCategoriesEndpoint(IConfigurationContainerLookup containerLookup)
        : base(containerLookup)
    {
    }
}
