using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Endpoints;
using Microsoft.Extensions.Logging;

namespace ReferenceAuth.Endpoints;

/// <summary>
/// Revokes a personal access token for the current user (DELETE /users/me/tokens/{tokenId}).
/// </summary>
public class RevokePersonalAccessTokenEndpoint : RevokePersonalAccessTokenEndpointBase
{
    private readonly IPersonalAccessTokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="RevokePersonalAccessTokenEndpoint"/> class.
    /// </summary>
    public RevokePersonalAccessTokenEndpoint(
        IPersonalAccessTokenService tokenService,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _tokenService = tokenService;
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult> RevokeToken(Guid userId, Guid tokenId, CancellationToken ct)
        => _tokenService.RevokeToken(userId, tokenId, ct);
}
