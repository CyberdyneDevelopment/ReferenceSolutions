using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ReferenceTenants.Endpoints.Logging;

namespace ReferenceTenants.Endpoints;

/// <summary>
/// Sets the caller's default tenant.
/// The user must already be a member of the requested tenant.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SetDefaultTenantEndpoint : Endpoint<SetDefaultTenantRequest, SetDefaultTenantResponse>
{
    private readonly UserTenantConfigurationProvider _userTenantProvider;
    private readonly ILogger<SetDefaultTenantEndpoint> _logger;

    /// <inheritdoc />
    public SetDefaultTenantEndpoint(
        UserTenantConfigurationProvider userTenantProvider,
        ILogger<SetDefaultTenantEndpoint>? logger)
    {
        _userTenantProvider = userTenantProvider;
        _logger = logger ?? NullLogger<SetDefaultTenantEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Put("/tenants/{id}/default");
        Policies("tenants:write");
        Tags("Tenants");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(SetDefaultTenantRequest req, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            await Send.UnauthorizedAsync(ct).ConfigureAwait(false);
            return;
        }

        var result = await _userTenantProvider.SetDefaultTenant(userId, req.Id, ct).ConfigureAwait(false);

        if (!result.IsSuccess)
        {
            var message = result.CurrentMessage;
            // Why: SetDefaultTenantNotMember produces a Warning-level message — return 403.
            if (message != null && message.Contains("not a member", StringComparison.OrdinalIgnoreCase))
            {
                TenantLog.SetDefaultTenantNotMember(_logger, userIdStr, req.Id);
                await Send.ForbiddenAsync(ct).ConfigureAwait(false);
                return;
            }

            TenantLog.SetDefaultTenantFailed(_logger, new InvalidOperationException(message), userIdStr, req.Id);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        TenantLog.DefaultTenantSet(_logger, userIdStr, req.Id);
        await Send.OkAsync(new SetDefaultTenantResponse { TenantId = req.Id }, ct).ConfigureAwait(false);
    }
}

/// <summary>Request model for setting the default tenant.</summary>
public sealed class SetDefaultTenantRequest
{
    /// <summary>Gets or sets the tenant identifier from the route.</summary>
    [BindFrom("id")]
    public Guid Id { get; set; }
}

/// <summary>Response model confirming the new default tenant.</summary>
public sealed class SetDefaultTenantResponse
{
    /// <summary>Gets or sets the tenant identifier that was set as default.</summary>
    public Guid TenantId { get; set; }
}
