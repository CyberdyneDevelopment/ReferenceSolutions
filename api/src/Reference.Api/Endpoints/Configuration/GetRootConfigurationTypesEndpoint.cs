using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to list root configuration types.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetRootConfigurationTypesEndpoint : Fdw.Operations.Endpoints.ConfigurationMetadata.GetRootConfigurationTypesEndpoint
{
    /// <inheritdoc />
    public GetRootConfigurationTypesEndpoint(IConfigurationContainerLookup containerLookup)
        : base(containerLookup)
    {
    }
}
