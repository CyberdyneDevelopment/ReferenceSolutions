using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;
using Fdw.Claims.Abstractions;
using Fdw.Operations.Abstractions.Audit;
using Fdw.Results;
using Fdw.Services.Users;
using Fdw.Web.RestEndpoints.ErrorMapping;
using FdwReferenceAuth.Endpoints.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuth.Endpoints;

/// <summary>
/// Clears the caller's default organization in the company they act in: <c>DELETE /organizations/default</c> (FDW-2938). The next sign-in
/// starts in the company with no department.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class ClearDefaultOrganizationEndpoint : EndpointWithoutRequest
{
    private readonly IUserTenantConfigurationProvider _userTenantProvider;
    private readonly IAdministrativeAudit _audit;
    private readonly ILogger<ClearDefaultOrganizationEndpoint> _logger;

    /// <inheritdoc />
    public ClearDefaultOrganizationEndpoint(
        IUserTenantConfigurationProvider userTenantProvider,
        IAdministrativeAudit audit,
        ILogger<ClearDefaultOrganizationEndpoint>? logger)
    {
        _userTenantProvider = userTenantProvider;
        _audit = audit;
        _logger = logger ?? NullLogger<ClearDefaultOrganizationEndpoint>.Instance;
    }

    /// <inheritdoc />
    public override void Configure()
    {
        Delete("/organizations/default");
        // Why "authenticated": the route only clears the CALLER's own default, in the company their token acts in.
        Policies("authenticated");
        Tags("Tenants");
    }

    /// <inheritdoc />
    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdText = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdText, out var userId)
            || !Guid.TryParse(User.FindFirst(ClaimDefinitions.tenantId.Name)?.Value, out var tenantId))
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(GenericResult.Failure(TenantLog.ClearDefaultOrganizationNotSignedIn(_logger)), HttpContext)).ConfigureAwait(false);
            return;
        }

        var cleared = await DefaultOrganizationMembership.Keep(_userTenantProvider, userId, tenantId, null, _logger, ct).ConfigureAwait(false);
        if (cleared.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(cleared, HttpContext)).ConfigureAwait(false);
            return;
        }

        var audited = await DefaultOrganizationMembership.Audit(_audit, "DefaultOrganizationCleared", userId, tenantId, null, _logger, ct).ConfigureAwait(false);
        if (audited.IsFailure)
        {
            await Send.ResultAsync(ResultHttpStatusMapper.Answer(audited, HttpContext)).ConfigureAwait(false);
            return;
        }

        TenantLog.DefaultOrganizationCleared(_logger, userIdText!, tenantId);
        await Send.NoContentAsync(ct).ConfigureAwait(false);
    }
}
