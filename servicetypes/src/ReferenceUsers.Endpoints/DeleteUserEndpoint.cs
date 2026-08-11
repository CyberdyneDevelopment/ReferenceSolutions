using System;
using System.Diagnostics.CodeAnalysis;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceUsers.Endpoints.Logging;

namespace ReferenceUsers.Endpoints;

/// <summary>
/// Endpoint to delete a user (Admin only).
/// </summary>
[ExcludeFromCodeCoverage]
public class DeleteUserEndpoint : DeleteUserEndpointBase
{
    private readonly ILogger<DeleteUserEndpoint> _logger;

    /// <inheritdoc />
    public DeleteUserEndpoint(
        UserConfigurationProvider userProvider,
        ILogger<DeleteUserEndpoint> logger)
        : base(userProvider)
    {
        _logger = logger ?? NullLogger<DeleteUserEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to DeleteUserEndpointBase.DeletePolicy ("users:delete"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s =>
        {
            s.Summary = "Delete a user";
            s.Description = "Deletes a user account. Requires Admin role.";
        Tags("Users");
        });
    }

    /// <inheritdoc />
    protected override void OnDeletingUser(Guid userId)
    {
        ApiLog.DeletingResource(_logger, $"user {userId}");
    }

    /// <inheritdoc />
    protected override void OnUserDeleted(Guid userId)
    {
        ApiLog.ResourceDeleted(_logger, $"user {userId}");
    }
}
