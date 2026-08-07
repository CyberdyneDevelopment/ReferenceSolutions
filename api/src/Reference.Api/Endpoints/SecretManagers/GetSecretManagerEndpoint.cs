using System.Diagnostics.CodeAnalysis;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.Abstractions;
using Fdw.Services.SecretManagers.Endpoints;

using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.SecretManagers;

/// <summary>
/// Gets a specific secret manager by name.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class GetSecretManagerEndpoint : GetSecretManagerEndpointBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSecretManagerEndpoint"/> class.
    /// </summary>
    public GetSecretManagerEndpoint(
        SecretManagerConfigurationProvider provider,
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
