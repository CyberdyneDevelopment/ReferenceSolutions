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
using Fdw.Messages;
using Fdw.Results;
using Fdw.Web.RestEndpoints.ErrorMapping;
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
        Post("/tenants/{Id}/default");
        // Why "authenticated" and not tenants:write: this route only ever sets the CALLER's own default (the id comes from their token and the memberships are theirs).
        Policies("authenticated");
        Tags("Tenants");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(SetDefaultTenantRequest req, CancellationToken ct)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(TenantLog.SetDefaultTenantNotSignedIn(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        // Why the memberships are read first: a tenant that does not exist and a tenant the caller does not belong to are one refusal, so the
        // answer never tells a person whether a tenant id exists. The tenant itself is read only for one the caller belongs to.
        var memberships = await _userTenantProvider
            .Find<IUserTenantImplementationConfiguration>(t => t.UserId == userId, ct)
            .ConfigureAwait(false);
        if (memberships.IsFailure)
        {
            TenantLog.SetDefaultTenantFailed(_logger, new InvalidOperationException(memberships.CurrentMessage), userIdStr, req.Id);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(memberships, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (memberships.Value is null)
        {
            TenantLog.SetDefaultTenantFailed(_logger, new InvalidOperationException(memberships.CurrentMessage), userIdStr, req.Id);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(TenantLog.SetDefaultTenantMembershipsReturnedNothing(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        if (!memberships.Value.Any(t => t.TenantId == req.Id))
        {
            await Refuse(TenantLog.SetDefaultTenantNotMember(_logger, userIdStr, req.Id)).ConfigureAwait(false);
            return;
        }

        var tenantResult = await _tenantProvider.GetTenant(req.Id, ct).ConfigureAwait(false);
        if (!tenantResult.IsSuccess)
        {
            TenantLog.SetDefaultTenantFailed(_logger, new InvalidOperationException(tenantResult.CurrentMessage), userIdStr, req.Id);
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(tenantResult, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (tenantResult.Value is null || !tenantResult.Value.IsSelectable)
        {
            await Refuse(TenantLog.SetDefaultTenantNotSelectable(_logger, userIdStr, req.Id)).ConfigureAwait(false);
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
                .Update(membership, UserTenantsDomain, UserTenantsDomain, membership.Name, ct)
                .ConfigureAwait(false);
            if (!saved.IsSuccess)
            {
                TenantLog.SetDefaultTenantFailed(_logger, new InvalidOperationException(saved.CurrentMessage), userIdStr, req.Id);
                await Send.ResultAsync(ResultHttpStatusMapper.Answer(saved, HttpContext)).ConfigureAwait(false);
                return;
            }
        }

        TenantLog.DefaultTenantSet(_logger, userIdStr, req.Id);
        await Send.OkAsync(new SetDefaultTenantResponse { TenantId = req.Id }, ct).ConfigureAwait(false);
    }

    // The refusal is answered with the status of its message's category (403) and the reason the message states for the caller.
    private Task<FastEndpoints.Void> Refuse(IGenericMessage reason) =>
        Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(reason), HttpContext));
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
