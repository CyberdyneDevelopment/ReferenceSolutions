using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Abstractions;
using Fdw.Services.Identity;
using Fdw.Services.Identity.Authentik;
using Fdw.Services.Identity.Endpoints;

namespace ReferenceIdentity.Endpoints;

/// <summary>
/// <c>POST identities/authentik-client-credentials</c> — creates an identity backed by an Authentik
/// service account.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateAuthentikClientCredentialsIdentityEndpoint
    : CreateIdentityEndpointBase<AuthentikClientCredentialsConfiguration, CreateAuthentikClientCredentialsIdentityRequest>
{
    private readonly IServiceConfigurationProvider<IdentityServiceConfiguration> _identities;
    private readonly IServiceConfigurationWriter<IdentityServiceConfiguration> _writer;

    /// <summary>Initializes a new instance of the class.</summary>
    /// <param name="identities">Reads existing identities, to reject a duplicate name.</param>
    /// <param name="writer">Persists the aggregate.</param>
    public CreateAuthentikClientCredentialsIdentityEndpoint(
        IServiceConfigurationProvider<IdentityServiceConfiguration> identities,
        IServiceConfigurationWriter<IdentityServiceConfiguration> writer)
    {
        _identities = identities;
        _writer = writer;
    }

    /// <inheritdoc />
    protected override IServiceConfigurationProvider<IdentityServiceConfiguration> Identities => _identities;

    /// <inheritdoc />
    protected override IServiceConfigurationWriter<IdentityServiceConfiguration> Writer => _writer;

    /// <inheritdoc />
    protected override string Route => "identities/authentik-client-credentials";

    /// <inheritdoc />
    protected override string EndpointSummary => "Create an Authentik client-credentials identity";

    /// <inheritdoc />
    protected override string EndpointDescription =>
        "Registers an identity that exchanges a client id and secret for a short-lived Authentik "
        + "token. The secret is named, not supplied: it is resolved through the named secret manager.";

    /// <inheritdoc />
    protected override AuthentikClientCredentialsConfiguration CreateTypedBody(
        CreateAuthentikClientCredentialsIdentityRequest request,
        Guid identityId) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            Name = request.Name,
            ServiceType = "Identity",
            SectionName = "Identities",
            ServiceOptionType = "AuthentikClientCredentials",
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
        AuthentikClientCredentialsConfiguration typedBody) =>
        new()
        {
            Id = identity.Id,
            Name = identity.Name,
            Mechanism = identity.ServiceOptionType,
            Description = identity.Description,
            Issuer = typedBody.Issuer,
        };
}
