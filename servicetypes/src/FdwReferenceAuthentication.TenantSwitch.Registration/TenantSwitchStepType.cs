using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuthentication.TenantSwitch.Registration;

/// <summary>
/// Establishes the caller of a tenant switch: the principal already authenticated on the request,
/// now asking to act in another tenant.
/// </summary>
/// <remarks>
/// <para>
/// A user acts in one tenant at a time — the tenant their token was issued for — and switching is
/// asking for a new token for another tenant (FDW-825). This step contributes only who is asking
/// and where to; it decides nothing. Whether they may act there is the issuance eligibility check
/// every token passes (<c>AuthorizeIssuance</c>): a member of that tenant, or a member of the global
/// tenant. What the new token carries is <c>BakePermissions</c>' ordinary answer — the roles held in
/// that tenant.
/// </para>
/// <para>
/// The identity is the one the caller already proved: it comes from the token the request was
/// authenticated with, never from the request body. The new token states no authentication methods
/// of its own, because this step verifies no credential.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(AuthenticationStepTypes), "TenantSwitch")]
public sealed class TenantSwitchStepType
    : AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
      IAuthenticationStep
{
    // Captured when the host is built: an option is created by its module initializer, which needs
    // a parameterless constructor, so what it needs arrives where a live container exists.
    private ITenantSwitchRequestAccessor? _request;
    private ILogger _logger = NullLogger<TenantSwitchStepType>.Instance;

    /// <summary>Initializes a new instance of the <see cref="TenantSwitchStepType"/> class.</summary>
    public TenantSwitchStepType()
        : base("TenantSwitch",
               "AuthenticationSteps",
               "Tenant Switch",
               "Re-establishes an already authenticated caller for another tenant they may act in")
    {
        Registration((builder, loggerFactory) =>
        {
            // What this step reads the switch request through. Registered here, not by the shared
            // collection, so a host that never references this package never pulls it in.
            builder.Services.TryAddSingleton<ITenantSwitchRequestAccessor, HttpTenantSwitchRequestAccessor>();
            builder.Services.AddHttpContextAccessor();

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        Initialization((host, loggerFactory) =>
        {
            _request = host.Services.GetRequiredService<ITenantSwitchRequestAccessor>();
            _logger = loggerFactory?.CreateLogger<TenantSwitchStepType>()
                ?? NullLogger<TenantSwitchStepType>.Instance;

            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Requires => [];

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Contributes => [ContextElements.Subject, ContextElements.Principal];

    /// <inheritdoc />
    /// <remarks>No credential is verified here, so no method is proved.</remarks>
    public IReadOnlyList<string> AuthenticationMethods => [];

    /// <inheritdoc />
    public Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default)
    {
        if (_request is null)
            return Task.FromResult(GenericResult<StepOutcome>.Failure(NotInitialized(_logger)));

        if (_request.CallerId is not { } callerId)
            return Task.FromResult(GenericResult<StepOutcome>.Failure(TenantSwitchLog.NoCaller(_logger)));

        if (_request.RequestedTenantId is not { } tenantId)
            return Task.FromResult(GenericResult<StepOutcome>.Failure(TenantSwitchLog.NoTenant(_logger, callerId)));

        TenantSwitchLog.Switching(_logger, callerId, tenantId);

        return Task.FromResult(GenericResult<StepOutcome>.Success(new StepOutcome.Contributed(new ContextContribution
        {
            Subject = new Subject
            {
                Issuer = AuthConstants.LocalIssuer,
                SubjectId = callerId.ToString(),
                AuthenticatedAt = DateTimeOffset.UtcNow,
            },

            // A department is stated only when the switch asked for one; a switch of tenant alone leaves the token without a department
            // (FDW-1802). The eligibility check that follows decides whether the person may act in it.
            Principal = new Principal { Id = callerId, TenantId = tenantId, OrganizationId = _request.RequestedOrganizationId },
        })));
    }
}
