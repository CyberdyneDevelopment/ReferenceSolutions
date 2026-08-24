using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Abstractions;
using Fdw.Services.Identity;
using Fdw.Services.Identity.ClientCredentials;
using Fdw.Services.Identity.Endpoints;

namespace ReferenceIdentity.Endpoints;

/// <summary>
/// <c>POST identities/client-credentials</c> — creates an identity that authenticates with an
/// OAuth 2.0 client-credentials grant against any conforming token endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateClientCredentialsIdentityEndpoint
    : CreateIdentityEndpointBase<ClientCredentialsConfiguration, CreateClientCredentialsIdentityRequest>
{
    private readonly IServiceConfigurationProvider<IdentityServiceConfiguration> _identities;

    /// <summary>Initializes a new instance of the class.</summary>
    /// <param name="identities">Reads existing identities and persists the aggregate.</param>
    public CreateClientCredentialsIdentityEndpoint(
        IServiceConfigurationProvider<IdentityServiceConfiguration> identities)
    {
        _identities = identities;
    }

    /// <inheritdoc />
    protected override IServiceConfigurationProvider<IdentityServiceConfiguration> Identities => _identities;

    /// <inheritdoc />
    protected override string Route => "identities/client-credentials";

    /// <inheritdoc />
    protected override string EndpointSummary => "Create a client-credentials identity";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Registers an identity that exchanges a client id and secret for a short-lived access "
        + "token. The secret is named, not supplied: it is resolved through the named secret manager.";

    /// <inheritdoc />
    protected override ClientCredentialsConfiguration CreateTypedBody(
        CreateClientCredentialsIdentityRequest request,
        Guid identityId) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            ServiceType = "Identity",
            SectionName = "Identities",
            ServiceOptionType = "ClientCredentials",
            Issuer = request.Issuer,
            TokenEndpoint = request.TokenEndpoint,
            ClientId = request.ClientId,
            SecretManagerName = request.SecretManagerName,
            SecretKeyName = request.SecretKeyName,
            Scopes = request.Scopes,
            Description = request.Description,
        };

    /// <inheritdoc />
    protected override IdentityDetailResponse MapToDetail(
        IdentityServiceConfiguration identity,
        ClientCredentialsConfiguration typedBody) =>
        new()
        {
            Id = identity.Id,
            Name = identity.Name,
            Mechanism = identity.ServiceOptionType,
            Description = identity.Description,
            Issuer = typedBody.Issuer,
        };
}
