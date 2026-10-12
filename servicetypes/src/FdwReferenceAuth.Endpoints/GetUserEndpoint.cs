using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Users.Configuration;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceAuth.Endpoints.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Endpoint to get a user by ID or username: POST /users/get (Admin only).
/// </summary>
[ExcludeFromCodeCoverage]
public class GetUserEndpoint : GetUserEndpointBase
{
    private readonly ILogger<GetUserEndpoint> _logger;

    /// <inheritdoc />
    public GetUserEndpoint(UserConfigurationProvider userProvider, ILogger<GetUserEndpoint> logger)
        : base(logger, userProvider) {
        _logger = logger ?? NullLogger<GetUserEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to GetUserEndpointBase.ReadPolicy ("users:read"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s =>
        {
            s.Summary = "Get user by ID or name";
            s.Description = "Reads one user by id or username from the users domain table. A read of one record is a POST. Requires Admin role.";
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
    protected override UserResponse MapToResponse(UserDomainConfiguration user)
    {
        return new UserResponse
        {
            Id = user.Id,
            Name = user.Name,
            Username = user.Name,
            Email = user.Email,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt.DateTime,
        };
    }
}
