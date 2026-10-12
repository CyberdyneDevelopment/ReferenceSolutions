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
/// Endpoint to list all users (Admin only).
/// </summary>
[ExcludeFromCodeCoverage]
public class ListUsersEndpoint : ListUsersEndpointBase
{
    private readonly ILogger<ListUsersEndpoint> _logger;

    /// <inheritdoc />
    public ListUsersEndpoint(UserConfigurationProvider userProvider, ILogger<ListUsersEndpoint> logger)
        : base(logger, userProvider) {
        _logger = logger ?? NullLogger<ListUsersEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to ListUsersEndpointBase.ReadPolicy ("users:read"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s =>
        {
            s.Summary = "List all users";
            s.Description = "Returns a list of all users. Requires Admin role.";
        Tags("Users");
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        ApiLog.FetchingData(_logger, "users");
        await base.HandleAsync(ct);
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
