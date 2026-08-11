using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Fdw.Services.Users.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceUsers.Endpoints.Logging;

namespace ReferenceUsers.Endpoints;

/// <summary>
/// Endpoint to get a user by ID (Admin only).
/// </summary>
[ExcludeFromCodeCoverage]
public class GetUserEndpoint : GetUserEndpointBase
{
    private readonly ILogger<GetUserEndpoint> _logger;

    /// <inheritdoc />
    public GetUserEndpoint(
        UserConfigurationProvider userProvider,
        ILogger<GetUserEndpoint> logger)
        : base(userProvider)
    {
        _logger = logger ?? NullLogger<GetUserEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to GetUserEndpointBase.ReadPolicy ("users:read"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s =>
        {
            s.Summary = "Get user by ID";
            s.Description = "Returns user information by ID. Requires Admin role.";
        Tags("Users");
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(UserScopedRequest req, CancellationToken ct)
    {
        ApiLog.GettingResource(_logger, $"user {req.IdOrName}");
        await base.HandleAsync(req, ct);
    }

    /// <inheritdoc />
    protected override UserResponse MapToResponse(IUser user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Username,
            Username = user.Username,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt.DateTime,
            LastLoginAt = user.LastLoginAt?.DateTime
        };
    }
}
