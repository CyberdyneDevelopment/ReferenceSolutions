using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Endpoints;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Auth;

/// <summary>
/// Creates a personal access token for the current user (POST /users/me/tokens).
/// </summary>
public sealed class CreatePersonalAccessTokenEndpoint : CreatePersonalAccessTokenEndpointBase
{
    private readonly IPersonalAccessTokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreatePersonalAccessTokenEndpoint"/> class.
    /// </summary>
    public CreatePersonalAccessTokenEndpoint(
        IPersonalAccessTokenService tokenService,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _tokenService = tokenService;
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<PersonalAccessTokenCreatedResult>> CreateToken(
        Guid userId, string label, DateTime? expiresAt, CancellationToken ct)
        => _tokenService.CreateToken(userId, label, expiresAt, ct);
}
