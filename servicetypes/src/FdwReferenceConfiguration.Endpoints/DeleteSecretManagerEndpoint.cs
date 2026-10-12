using System.Diagnostics.CodeAnalysis;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints;

using Microsoft.Extensions.Logging;

namespace FdwReferenceSecretManagers.Endpoints;

/// <summary>
/// Deletes a secret manager configuration.
/// </summary>
[ExcludeFromCodeCoverage]
public class DeleteSecretManagerEndpoint : DeleteSecretManagerEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DeleteSecretManagerEndpoint"/> class.
    /// </summary>
    public DeleteSecretManagerEndpoint(
        ISecretManagerConfigurationProvider provider,
        ILogger<DeleteSecretManagerEndpoint>? logger = null)
        : base(provider, logger!)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("SecretManagers");
    }
}
