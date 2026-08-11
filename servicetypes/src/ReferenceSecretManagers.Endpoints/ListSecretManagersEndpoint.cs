using System.Diagnostics.CodeAnalysis;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints;

using Microsoft.Extensions.Logging;

namespace ReferenceSecretManagers.Endpoints;

/// <summary>
/// Lists all configured secret managers.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListSecretManagersEndpoint : ListSecretManagersEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ListSecretManagersEndpoint"/> class.
    /// </summary>
    public ListSecretManagersEndpoint(
        SecretManagerConfigurationProvider provider,
        ILogger<ListSecretManagersEndpoint>? logger = null)
        : base(provider, logger!)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("SecretManagers");
    }
}
