using Fdw.Services.Users.Configuration;
using System.Linq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using FdwReferenceAuth.Endpoints.Logging;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Sets the caller's default tenant.
/// The user must already be a member of the requested tenant, and the tenant
/// must be selectable -- structural sentinel tenants (default, system) can
/// never be switched into even by a member.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SetDefaultTenantEndpoint : Endpoint<SetDefaultTenantRequest, SetDefaultTenantResponse>
{
    /// <summary>The UserTenants domain, and its one implementation, as the domain row records them.</summary>
    private const string UserTenantsDomain = "UserTenants";

    private readonly UserTenantConfigurationProvider _userTenantProvider;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<SetDefaultTenantEndpoint> _logger;

    /// <inheritdoc />
    public SetDefaultTenantEndpoint(
        UserTenantConfigurationProvider userTenantProvider,
        ITenantProvider tenantProvider,
        ILogger<SetDefaultTenantEndpoint>? logger)
    {
        _userTenantProvider = userTenantProvider;
        _tenantProvider = tenantProvider;
        _logger = logger ?? NullLogger<SetDefaultTenantEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/tenants/{id}/default");
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

        var tenantResult = await _tenantProvider.GetTenant(req.Id, ct).ConfigureAwait(false);
        if (!tenantResult.IsSuccess)
        {
            TenantLog.SetDefaultTenantFailed(_logger, new InvalidOperationException(tenantResult.CurrentMessage), userIdStr, req.Id);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (tenantResult.Value is null || !tenantResult.Value.IsSelectable)
        {
            TenantLog.SetDefaultTenantNotMember(_logger, userIdStr, req.Id);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        var memberships = await _userTenantProvider
            .Find<IUserTenantImplementationConfiguration>(t => t.UserId == userId, ct)
            .ConfigureAwait(false);
        if (!memberships.IsSuccess || memberships.Value is null)
        {
            TenantLog.SetDefaultTenantFailed(_logger, new InvalidOperationException(memberships.CurrentMessage), userIdStr, req.Id);
            await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
            return;
        }

        if (!memberships.Value.Any(t => t.TenantId == req.Id))
        {
            TenantLog.SetDefaultTenantNotMember(_logger, userIdStr, req.Id);
            await Send.ForbiddenAsync(ct).ConfigureAwait(false);
            return;
        }

        // Why demotions first: a user may have one default membership, so the old default is cleared
        // before the new one is set -- promoting first would briefly hold two.
        var changes = memberships.Value
            .Where(t => t.IsDefault != (t.TenantId == req.Id))
            .OrderBy(t => t.TenantId == req.Id);
        foreach (var membership in changes)
        {
            membership.IsDefault = membership.TenantId == req.Id;
            var saved = await _userTenantProvider
                .Save(membership, UserTenantsDomain, UserTenantsDomain, membership.Name, ct)
                .ConfigureAwait(false);
            if (!saved.IsSuccess)
            {
                TenantLog.SetDefaultTenantFailed(_logger, new InvalidOperationException(saved.CurrentMessage), userIdStr, req.Id);
                await Send.ErrorsAsync(500, ct).ConfigureAwait(false);
                return;
            }
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
