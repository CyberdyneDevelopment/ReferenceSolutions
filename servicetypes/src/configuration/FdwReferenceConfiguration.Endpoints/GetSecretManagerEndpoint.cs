using System.Diagnostics.CodeAnalysis;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints;

using Microsoft.Extensions.Logging;

namespace FdwReferenceSecretManagers.Endpoints;

/// <summary>
/// Gets a specific secret manager by name.
/// </summary>
[ExcludeFromCodeCoverage]
public class GetSecretManagerEndpoint : GetSecretManagerEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSecretManagerEndpoint"/> class.
    /// </summary>
    public GetSecretManagerEndpoint(
        ISecretManagerConfigurationProvider provider,
        ILogger<GetSecretManagerEndpoint>? logger = null)
        : base(provider, logger!)
    {
    }

    /// <inheritdoc/>
    protected override void ConfigureEndpoint()
    {
        Tags("SecretManagers");
    }
}
