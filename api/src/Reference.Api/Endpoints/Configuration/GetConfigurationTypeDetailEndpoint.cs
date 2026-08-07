using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Data.Abstractions;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint to get configuration type detail.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetConfigurationTypeDetailEndpoint : Fdw.Operations.Endpoints.ConfigurationMetadata.GetConfigurationTypeDetailEndpoint
{
    /// <inheritdoc />
    public GetConfigurationTypeDetailEndpoint(IConfigurationContainerLookup containerLookup)
        : base(containerLookup)
    {
    }
}
