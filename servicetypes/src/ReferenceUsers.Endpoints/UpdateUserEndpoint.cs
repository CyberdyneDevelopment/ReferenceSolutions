using System.Diagnostics.CodeAnalysis;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Services.Users;
using Fdw.Services.Users.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using ReferenceUsers.Endpoints.Logging;

namespace ReferenceUsers.Endpoints;

/// <summary>
/// Endpoint to update a user (Admin only).
/// </summary>
[ExcludeFromCodeCoverage]
public class UpdateUserEndpoint : UpdateUserEndpointBase<UpdateUserRequest>
{
    private readonly ILogger<UpdateUserEndpoint> _logger;

    /// <inheritdoc />
    public UpdateUserEndpoint(
        UserConfigurationProvider userProvider,
        ILogger<UpdateUserEndpoint> logger)
        : base(userProvider)
    {
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<UpdateUserEndpoint>.Instance;
    }

    /// <inheritdoc />
    protected override void ConfigureEndpoint()
    {
        // Why: policy moved to UpdateUserEndpointBase.AdminPolicy ("users:delete"), applied by the base's Configure().
        // Re-declaring Policies() here would AND a second requirement onto it, not replace it.
        Summary(s =>
        {
            s.Summary = "Update a user";
            s.Description = "Updates user information. Requires Admin role.";
        Tags("Users");
        });
    }

    /// <inheritdoc />
    public override async Task HandleAsync(UpdateUserRequest req, CancellationToken ct)
    {
        ApiLog.UpdatingResource(_logger, $"user {req.Name}");

        // Why: handle the not-found path here so callers see 404 + structured envelope instead of
        // the base UpdateUserEndpointBase blanket 500 on every IGenericResult.Failure.
        var userResult = await UserProvider.GetUser(req.Name, ct).ConfigureAwait(false);
        if (userResult.IsFailure || userResult.Value is null)
        {
            HttpContext.Response.StatusCode = 404;
            HttpContext.Response.ContentType = "application/json";
            await HttpContext.Response.WriteAsJsonAsync(new
            {
                errorCode = "NotFound",
                messages = new[] { $"User '{req.Name}' was not found." }
            }, ct).ConfigureAwait(false);
            return;
        }

        await base.HandleAsync(req, ct);
    }

    /// <inheritdoc />
    // Why: request binds {Name} as string; resolve the user by name first to get the full
    // UserConfiguration record, then call UpdateUser which keys on the record's Id.
    protected override async Task<IGenericResult> Update(UpdateUserRequest request, CancellationToken ct)
    {
        var userResult = await UserProvider.GetUser(request.Name, ct).ConfigureAwait(false);
        if (userResult.IsFailure || userResult.Value == null)
        {
            return GenericResult.Failure(userResult.Messages.ToArray());
        }

        return await UserProvider.UpdateUser(userResult.Value, ct).ConfigureAwait(false);
    }
}
