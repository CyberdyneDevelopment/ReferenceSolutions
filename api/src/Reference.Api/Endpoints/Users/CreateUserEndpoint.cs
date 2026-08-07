using System.Diagnostics.CodeAnalysis;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Authorization;
using Fdw.Services.Users.Abstractions;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Reference.Api.Logging;
using Fdw.Services.Users.Clients.Models;

namespace Reference.Api.Endpoints;

/// <summary>
/// Endpoint to create a new user (Admin only).
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class CreateUserEndpoint : CreateUserEndpointBase<CreateUserRequest>
{
    private readonly ILogger<CreateUserEndpoint> _logger;

    /// <inheritdoc />
    public CreateUserEndpoint(
        UserConfigurationProvider userProvider,
        UserTenantConfigurationProvider tenantProvider,
        UserRoleConfigurationProvider userRoleProvider,
        RoleConfigurationProvider roleProvider,
        IUserCredentialService credentialService,
        ILogger<CreateUserEndpoint> logger)
        : base(userProvider, tenantProvider, userRoleProvider, roleProvider, credentialService)
    {
        _logger = logger ?? NullLogger<CreateUserEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        Summary(s =>
        {
            s.Summary = "Create a new user";
            s.Description = "Creates a new user account. Requires Admin role.";
            s.ExampleRequest = new CreateUserRequest
            {
                Username = "newuser",
                Password = "SecurePassword123#",
                Email = "newuser@example.com",
                Roles = ["User"]
            };
        });
        Tags("Users");
    }

    /// <inheritdoc />
    protected override void OnCreatingUser(string username)
    {
        ApiLog.CreatingResource(_logger, $"user {username} by admin {User.Identity?.Name}");
    }

    /// <inheritdoc />
    protected override void OnUserCreated(string username, Guid userId)
    {
        ApiLog.ResourceCreated(_logger, $"user {username} with ID {userId}");
    }
}
