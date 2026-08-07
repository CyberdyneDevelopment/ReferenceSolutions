using System.Diagnostics.CodeAnalysis;
using Fdw.Services.SecretManagers.Endpoints;

namespace Reference.Api.Endpoints;

/// <summary>
/// Concrete endpoint for listing registered secret manager types.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ListSecretManagerTypesEndpoint : ListSecretManagerTypesEndpointBase
{
    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("SecretManagers");
    }
}
