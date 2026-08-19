using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Abstractions;
using Fdw.Services.Identity;
using Fdw.Services.Identity.Endpoints;

namespace ReferenceIdentity.Endpoints;

/// <summary><c>GET identities</c> — lists the configured managed identities.</summary>
[ExcludeFromCodeCoverage]
public sealed class ListIdentitiesEndpoint : ListIdentitiesEndpointBase
{
    private readonly IServiceConfigurationProvider<IdentityServiceConfiguration> _identities;

    /// <summary>Initializes a new instance of the <see cref="ListIdentitiesEndpoint"/> class.</summary>
    /// <param name="identities">Reads the configured identities.</param>
    public ListIdentitiesEndpoint(IServiceConfigurationProvider<IdentityServiceConfiguration> identities) =>
        _identities = identities;

    /// <inheritdoc />
    protected override IServiceConfigurationProvider<IdentityServiceConfiguration> Identities => _identities;
}
