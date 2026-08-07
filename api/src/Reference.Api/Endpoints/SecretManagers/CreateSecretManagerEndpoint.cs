using System.Diagnostics.CodeAnalysis;

using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints;

using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.SecretManagers;

/// <summary>
/// Creates a new secret manager configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateSecretManagerEndpoint : CreateSecretManagerEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateSecretManagerEndpoint"/> class.
    /// </summary>
    public CreateSecretManagerEndpoint(
        SecretManagerConfigurationProvider provider,
        ILogger<CreateSecretManagerEndpoint>? logger = null)
        : base(provider, logger!)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("SecretManagers");
    }
}
