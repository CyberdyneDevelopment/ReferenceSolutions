using System.Diagnostics.CodeAnalysis;

using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints;

using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.SecretManagers;

/// <summary>
/// Updates an existing secret manager configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class UpdateSecretManagerEndpoint : UpdateSecretManagerEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSecretManagerEndpoint"/> class.
    /// </summary>
    public UpdateSecretManagerEndpoint(
        SecretManagerConfigurationProvider provider,
        ILogger<UpdateSecretManagerEndpoint>? logger = null)
        : base(provider, logger!)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("SecretManagers");
    }
}
