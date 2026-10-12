using Fdw.Services.Identity.Abstractions;
using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Identity.ClientCredentials;
using Fdw.Services.Identity.Endpoints;
using Microsoft.Extensions.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// <c>POST identities/client-credentials</c> — creates an identity that authenticates with an
/// OAuth 2.0 client-credentials grant against any conforming token endpoint.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateClientCredentialsIdentityEndpoint
    : CreateIdentityEndpointBase<ClientCredentialsConfiguration, CreateClientCredentialsIdentityRequest>
{
    private readonly IIdentityServiceConfigurationProvider _identities;

    /// <summary>Initializes a new instance of the class.</summary>
    /// <param name="logger">The logger.</param>
    /// <param name="identities">Reads existing identities and persists the aggregate.</param>
    public CreateClientCredentialsIdentityEndpoint(
        ILogger<CreateClientCredentialsIdentityEndpoint> logger,
        IIdentityServiceConfigurationProvider identities)
        : base(logger)
    {
        _identities = identities;
    }

    /// <inheritdoc />
    protected override IIdentityServiceConfigurationProvider Identities => _identities;

    /// <inheritdoc />
    /// <summary>The implementation this endpoint creates, named as the Identity domain row records it.</summary>
    private const string Mechanism = "ClientCredentials";

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
            Issuer = request.Issuer,
            TokenEndpoint = request.TokenEndpoint,
            ClientId = request.ClientId,
            SecretManagerName = request.SecretManagerName,
            SecretKeyName = request.SecretKeyName,
            Scopes = request.Scopes,
            Description = request.Description,
        };

    /// <inheritdoc />
    protected override IdentityDetailResponse MapToDetail(ClientCredentialsConfiguration typedBody) =>
        new()
        {
            Id = typedBody.Id,
            Name = typedBody.Name,
            Mechanism = Mechanism,
            Description = typedBody.Description,
            Issuer = typedBody.Issuer,
        };
}
