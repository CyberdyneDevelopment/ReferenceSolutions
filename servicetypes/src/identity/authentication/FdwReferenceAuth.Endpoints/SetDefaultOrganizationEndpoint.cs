using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Messages;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Multitenancy.Abstractions;
using Fdw.Services.Users;
using Fdw.Web.RestEndpoints.ErrorMapping;
using FdwReferenceAuth.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Sets the caller's default organization in the company they act in: <c>POST /organizations/{Id}/default</c> (FDW-2938). The next sign-in issues
/// a token already acting in it.
/// </summary>
/// <remarks>
/// The caller sets only their own, and only among the organizations of the company they act in where they hold access (the question the switch
/// asks). An organization that does not exist, one of another company, a deleted one and one the caller holds no access in are refused with one
/// answer, so it never says whether an organization id exists.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class SetDefaultOrganizationEndpoint : Endpoint<SetDefaultOrganizationRequest, SetDefaultOrganizationResponse>
{
    private readonly UserTenantConfigurationProvider _userTenantProvider;
    private readonly IOrganizationProvider _organizations;
    private readonly ITenantProvider _tenants;
    private readonly IAdministrativeAudit _audit;
    private readonly ILogger<SetDefaultOrganizationEndpoint> _logger;

    /// <inheritdoc />
    public SetDefaultOrganizationEndpoint(
        UserTenantConfigurationProvider userTenantProvider,
        IOrganizationProvider organizations,
        ITenantProvider tenants,
        IAdministrativeAudit audit,
        ILogger<SetDefaultOrganizationEndpoint>? logger)
    {
        _userTenantProvider = userTenantProvider;
        _organizations = organizations;
        _tenants = tenants;
        _audit = audit;
        _logger = logger ?? NullLogger<SetDefaultOrganizationEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Post("/organizations/{Id}/default");
        // Why "authenticated": the route only sets the CALLER's own default, in the company their token acts in.
        Policies("authenticated");
        Tags("Tenants");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(SetDefaultOrganizationRequest req, CancellationToken ct)
    {
        var userIdText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdText, out var userId)
            || !Guid.TryParse(User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value, out var tenantId))
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(TenantLog.SetDefaultOrganizationNotSignedIn(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var held = await Held(userId, tenantId, req.Id, ct).ConfigureAwait(false);
        if (held.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(held, HttpContext)).ConfigureAwait(false);
            return;
        }

        if (!held.Value)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(
                GenericResult.Failure(TenantLog.SetDefaultOrganizationNotHeld(_logger, userIdText!, req.Id)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var saved = await DefaultOrganizationMembership.Keep(_userTenantProvider, userId, tenantId, req.Id, _logger, ct).ConfigureAwait(false);
        if (saved.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(saved, HttpContext)).ConfigureAwait(false);
            return;
        }

        var audited = await DefaultOrganizationMembership.Audit(_audit, "DefaultOrganizationSet", userId, tenantId, req.Id, _logger, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(audited, HttpContext)).ConfigureAwait(false);
            return;
        }

        TenantLog.DefaultOrganizationSet(_logger, userIdText!, req.Id, tenantId);
        await Send.OkAsync(new SetDefaultOrganizationResponse { OrganizationId = req.Id }, ct).ConfigureAwait(false);
    }

    // True when the organization exists, is not deleted, is the company's, and the caller holds access in it; a failure only when a read failed.
    private async Task<IGenericResult<bool>> Held(Guid userId, Guid tenantId, Guid organizationId, CancellationToken ct)
    {
        var read = await _organizations.Get(organizationId, ct).ConfigureAwait(false);
        if (read.IsFailure && !FailureCategory.IsMissing(read))
        {
            return read.ToNewResult<bool>();
        }

        if (read.IsFailure || read.Value is not { IsDeleted: false } organization || organization.TenantId != tenantId)
        {
            return GenericResult<bool>.Success(false);
        }

        return await _tenants.ValidateOrganizationAccess(tenantId, organizationId, userId.ToString(), ct).ConfigureAwait(false);
    }
}

/// <summary>Request model for setting the default organization.</summary>
public sealed class SetDefaultOrganizationRequest
{
    /// <summary>Gets or sets the organization identifier from the route.</summary>
    [BindFrom("id")]
    public Guid Id { get; set; }
}

/// <summary>Response model confirming the new default organization.</summary>
public sealed class SetDefaultOrganizationResponse
{
    /// <summary>Gets or sets the organization the caller now lands in at sign-in.</summary>
    public Guid OrganizationId { get; set; }
}
