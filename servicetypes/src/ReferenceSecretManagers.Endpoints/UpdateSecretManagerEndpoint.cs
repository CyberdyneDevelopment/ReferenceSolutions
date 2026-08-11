using System.Diagnostics.CodeAnalysis;

using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints;

using Microsoft.Extensions.Logging;

namespace ReferenceSecretManagers.Endpoints;

/// <summary>
/// Updates an existing secret manager configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateSecretManagerEndpoint : UpdateSecretManagerEndpointBase
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
