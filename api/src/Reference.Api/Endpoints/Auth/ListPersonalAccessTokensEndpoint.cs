using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authentication.Abstractions.Methods;
using Fdw.Services.Authentication.Endpoints;
using Microsoft.Extensions.Logging;

namespace Reference.Api.Endpoints.Auth;

/// <summary>
/// Lists the current user's personal access tokens (GET /users/me/tokens).
/// </summary>
public sealed class ListPersonalAccessTokensEndpoint : ListPersonalAccessTokensEndpointBase
{
    private readonly IPersonalAccessTokenService _tokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ListPersonalAccessTokensEndpoint"/> class.
    /// </summary>
    public ListPersonalAccessTokensEndpoint(
        IPersonalAccessTokenService tokenService,
        ILoggerFactory loggerFactory)
        : base(loggerFactory)
    {
        _tokenService = tokenService;
    }

    /// <inheritdoc/>
    protected override Task<IGenericResult<IReadOnlyList<PersonalAccessTokenSummary>>> ListTokens(
        Guid userId, CancellationToken ct)
        => _tokenService.ListTokens(userId, ct);
}
