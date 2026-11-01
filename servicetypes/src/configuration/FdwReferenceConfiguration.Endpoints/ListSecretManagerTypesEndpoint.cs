using System.Diagnostics.CodeAnalysis;
using Fdw.Services.SecretManagers.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceSecretManagers.Endpoints;

/// <summary>
/// Concrete endpoint for listing registered secret manager types.
/// </summary>
[ExcludeFromCodeCoverage]
public class ListSecretManagerTypesEndpoint : ListSecretManagerTypesEndpointBase
{
    /// <summary>Initializes a new instance of the <see cref="ListSecretManagerTypesEndpoint"/> class.</summary>
    public ListSecretManagerTypesEndpoint(ILogger<ListSecretManagerTypesEndpoint> logger) : base(logger)
    {
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Tags("SecretManagers");
    }
}
