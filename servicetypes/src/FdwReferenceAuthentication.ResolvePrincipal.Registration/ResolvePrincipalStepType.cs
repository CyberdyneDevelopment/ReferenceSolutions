using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Text.Json;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authentication.Abstractions.Security;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.ExternalIdentityProviders.Abstractions;
using Fdw.Services.ExternalIdentityProviders.Binding;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuthentication.ResolvePrincipal.Registration;

/// <summary>
/// Binds a subject an authority proved to a principal this platform knows — provisioning one
/// just-in-time when a deployment has configured that for the subject's issuer.
/// </summary>
/// <remarks>
/// <para>
/// The half of federated login that the authority cannot do: it proved who the caller is to ITSELF,
/// and has no idea which of our principals that corresponds to. Every federated step therefore
/// contributes a Subject and stops, and this turns that into a Principal.
/// </para>
/// <para>
/// It stays a step of its own rather than folding into each authority's step, because the mapping
/// is the same wherever the subject came from — and because <c>Requires</c> Subject and
/// <c>Contributes</c> Principal is what stops a flow minting a token for nobody.
/// </para>
/// <para>
/// An unbound subject is not automatically a refusal: <see cref="ExternalIdentityProvisionerBindingConfigurationProvider"/>
/// is consulted for a provisioner bound to the subject's issuer. No binding row is the default-OFF
/// outcome and refuses exactly as before — provisioning an account for a caller this platform has
/// never decided to trust is not something an absence of configuration should do. A bound issuer
/// hands the subject and every claim contributed so far to that provisioner, which decides what (if
/// anything) to create; this step never inspects claim content itself; that policy lives entirely in
/// the provisioner a deployment configures.
/// </para>
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(AuthenticationStepTypes), "ResolvePrincipal")]
public sealed class ResolvePrincipalStepType
    : AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
      IConfiguredAuthenticationStep
{
    // Captured when the host is built: an option is created by its module initializer, which needs
    // a parameterless constructor, so what it needs arrives where a live container exists.
    private IPrincipalBinding? _bindings;
    private IExternalIdentityProvisionerBindingConfigurationProvider? _provisionerBindings;
    private IExternalIdentityProvisionerServiceProvider? _provisioners;
    private ITenantResolver? _tenants;
    private IAuthenticationContextAccessor? _authContextAccessor;
    private ILogger _logger = NullLogger<ResolvePrincipalStepType>.Instance;

    /// <summary>Initializes a new instance of the <see cref="ResolvePrincipalStepType"/> class.</summary>
    public ResolvePrincipalStepType()
        : base("ResolvePrincipal",
               "AuthenticationSteps",
               "Resolve Principal",
               "Binds a subject an authority proved to a principal this platform knows")
    {
        Initialization((host, loggerFactory) =>
        {
            _authContextAccessor = host.Services.GetRequiredService<IAuthenticationContextAccessor>();
            _bindings = host.Services.GetRequiredService<IPrincipalBinding>();
            _provisionerBindings = host.Services.GetRequiredService<IExternalIdentityProvisionerBindingConfigurationProvider>();
            _provisioners = host.Services.GetRequiredService<IExternalIdentityProvisionerServiceProvider>();
            _tenants = host.Services.GetRequiredService<ITenantResolver>();
            _logger = loggerFactory?.CreateLogger<ResolvePrincipalStepType>()
                ?? NullLogger<ResolvePrincipalStepType>.Instance;

            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Requires => [ContextElements.Subject];

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Contributes => [ContextElements.Principal];

    /// <inheritdoc />
    /// <remarks>Resolution proves nothing — the subject was already proved by whatever ran before.</remarks>
    public IReadOnlyList<string> AuthenticationMethods => [];

    /// <inheritdoc />
    /// <remarks>
    /// No settings is the step as it always was: bound subjects resolve, and an unbound one is left to
    /// provisioning. Settings exist only to turn on invitations, so settings that do not describe them
    /// completely are refused when the flow loads rather than read as "no invitations".
    /// </remarks>
    public IGenericResult ValidateConfiguration(string? configuration)
        => configuration is null ? GenericResult.Success() : Settings(configuration);

    /// <inheritdoc />
    public Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default)
        => Execute(context, configuration: null, tenants, cancellationToken);

    /// <inheritdoc />
    public async Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, string? configuration, ITenantProvider tenants, CancellationToken cancellationToken = default)
    {
        InvitationRedemption? invitations = null;
        if (configuration is not null)
        {
            var settings = Settings(configuration);
            if (settings.IsFailure)
                return settings.ToNewResult<StepOutcome>();
            invitations = settings.Value!.Invitations;
        }

        if (_bindings is null || _provisionerBindings is null || _provisioners is null || _tenants is null || _authContextAccessor is null)
            return GenericResult<StepOutcome>.Failure(NotInitialized(_logger));

        // Only this trusted built-in authentication operation runs under system context.
        using var systemScope = new SystemAuthenticationContextScope(_authContextAccessor);

        var subject = context.Subject!;

        var bound = await _bindings
            .Resolve(subject.Issuer, subject.SubjectId, cancellationToken)
            .ConfigureAwait(false);

        if (bound.IsFailure)
            return bound.ToNewResult<StepOutcome>();

        if (bound.Value is { } existing)
        {
            ResolvePrincipalLog.PrincipalResolved(_logger, subject.Issuer, existing.Id);
            return GenericResult<StepOutcome>.Success(
                new StepOutcome.Contributed(new ContextContribution { Principal = existing }));
        }

        // Unbound: an operator's invitation comes before provisioning, because the account it names
        // was made for this person on purpose; provisioning would make them a second one (FDW-910).
        if (invitations is not null
            && VerifiedEmail(context, invitations, subject.Issuer) is { } email)
        {
            var invited = await _bindings.RedeemInvitation(subject.Issuer, subject.SubjectId, email, cancellationToken)
                .ConfigureAwait(false);
            if (invited.IsFailure)
                return invited.ToNewResult<StepOutcome>();

            if (invited.Value is { } redeemed)
            {
                ResolvePrincipalLog.PrincipalResolved(_logger, subject.Issuer, redeemed.Id);
                return GenericResult<StepOutcome>.Success(
                    new StepOutcome.Contributed(new ContextContribution { Principal = redeemed }));
            }
        }

        // Unbound: consult provisioning before refusing. No matching binding row is the default-OFF
        // outcome, and refuses exactly as this step always has — an absence of configuration must
        // never be read as permission to create an account.
        return await TryProvision(subject, context, tenants, cancellationToken).ConfigureAwait(false);
    }

    // The address the issuer asserted, when this flow can trust it was verified; null otherwise. Only
    // a claim the issuer itself stated is read, and a subject asserting two different addresses has
    // none: picking one would be guessing which person they are.
    private string? VerifiedEmail(AuthenticationContext context, InvitationRedemption invitations, string issuer)
    {
        var addresses = context.Claims.Claims
            .Where(c => c.Source == ClaimSources.External
                && string.Equals(c.Type, invitations.EmailClaim, StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(c.Value))
            .Select(c => c.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (addresses.Count != 1)
        {
            ResolvePrincipalLog.InvitationAddressUnusable(_logger, issuer, addresses.Count);
            return null;
        }

        var verified = invitations.TrustIssuerEmail
            || context.Claims.Claims.Any(c => c.Source == ClaimSources.External
                && string.Equals(c.Type, invitations.EmailVerifiedClaim, StringComparison.Ordinal)
                && string.Equals(c.Value, "true", StringComparison.OrdinalIgnoreCase));

        if (!verified)
        {
            ResolvePrincipalLog.InvitationAddressUnverified(_logger, issuer);
            return null;
        }

        return addresses[0];
    }

    private IGenericResult<ResolvePrincipalStepConfiguration> Settings(string configuration)
    {
        ResolvePrincipalStepConfiguration? settings;
        try
        {
            settings = JsonSerializer.Deserialize<ResolvePrincipalStepConfiguration>(configuration);
        }
        catch (JsonException ex)
        {
            return GenericResult<ResolvePrincipalStepConfiguration>.Failure(
                ResolvePrincipalLog.ConfigurationNotJson(_logger, ex.Message));
        }

        // Exactly one way of knowing the address was verified; naming both, or neither, is a flow
        // whose operator has not decided, and a default here would decide for them.
        if (settings?.Invitations is not { } invitations
            || string.IsNullOrWhiteSpace(invitations.EmailClaim)
            || string.IsNullOrWhiteSpace(invitations.EmailVerifiedClaim) != invitations.TrustIssuerEmail)
            return GenericResult<ResolvePrincipalStepConfiguration>.Failure(ResolvePrincipalLog.InvitationsIncomplete(_logger));

        return GenericResult<ResolvePrincipalStepConfiguration>.Success(settings);
    }

    private async Task<IGenericResult<StepOutcome>> TryProvision(
        Subject subject, AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken)
    {
        // The platform's binding for this issuer is the one owned by the global tenant, and matching
        // is exact. It used to be the row whose TenantId was null, but every tenant column is NOT NULL
        // and the seed writes the global tenant's id, so a null test matched nothing and every
        // first-time sign-in was refused as unbound (FDW-825).
        var allTenants = await tenants.GetAllTenants(cancellationToken).ConfigureAwait(false);
        if (allTenants.IsFailure)
            return allTenants.ToNewResult<StepOutcome>();

        if ((allTenants.Value ?? []).SingleOrDefault(t => t.IsGlobal) is not { } globalTenant)
            return GenericResult<StepOutcome>.Failure(ResolvePrincipalLog.GlobalTenantMissing(_logger, subject.Issuer));

        var bindings = await _provisionerBindings!
            .Find<Fdw.Services.ExternalIdentityProviders.Binding.ExternalIdentityProvisionerBindingImplementationConfiguration>(
                candidate => candidate.TenantId == globalTenant.Id
                    && string.Equals(candidate.ProviderName, subject.Issuer, StringComparison.Ordinal),
                cancellationToken)
            .ConfigureAwait(false);

        if (bindings.IsFailure)
            return bindings.ToNewResult<StepOutcome>();

        // A subject with no binding AND no configured provisioner is a caller this platform has
        // never seen. Refusing is the whole point of the step: the alternative is inventing a
        // principal for them.
        if (bindings.Value is not [var binding] || binding.ProvisionerName is not { Length: > 0 } name)
            return GenericResult<StepOutcome>.Failure(ResolvePrincipalLog.NoBinding(_logger, subject.Issuer));

        var provisioner = await _provisioners!.Get(name, cancellationToken).ConfigureAwait(false);
        if (provisioner.IsFailure)
            return provisioner.ToNewResult<StepOutcome>();

        if (provisioner.Value is not { } resolvedProvisioner)
            return GenericResult<StepOutcome>.Failure(
                ResolvePrincipalLog.ProvisionerNotResolved(_logger, subject.Issuer, name));

        var provisioned = await resolvedProvisioner
            .Provision(subject.Issuer, subject.SubjectId, ToClaimsPrincipal(subject, context), cancellationToken)
            .ConfigureAwait(false);

        if (provisioned.IsFailure)
            return provisioned.ToNewResult<StepOutcome>();

        var tenant = await _tenants!.TenantFor(provisioned.Value, cancellationToken).ConfigureAwait(false);
        if (tenant.IsFailure)
            return tenant.ToNewResult<StepOutcome>();

        ResolvePrincipalLog.PrincipalProvisioned(_logger, subject.Issuer, name, provisioned.Value);

        return GenericResult<StepOutcome>.Success(new StepOutcome.Contributed(new ContextContribution
        {
            Principal = new Principal { Id = provisioned.Value, TenantId = tenant.Value },
        }));
    }

    /// <summary>Builds the claims a provisioner reads to decide what to create.</summary>
    /// <remarks>
    /// The subject itself is included as <c>sub</c>/<c>iss</c> alongside whatever the proving step
    /// contributed, since a provisioner may key off the subject identifier and not only the claims
    /// riding beside it.
    /// </remarks>
    private static ClaimsPrincipal ToClaimsPrincipal(Subject subject, AuthenticationContext context)
    {
        var identity = new ClaimsIdentity(authenticationType: subject.Issuer);
        identity.AddClaim(new System.Security.Claims.Claim(ClaimTypes.NameIdentifier, subject.SubjectId, valueType: null, issuer: subject.Issuer));

        foreach (var claim in context.Claims.Claims)
            identity.AddClaim(new System.Security.Claims.Claim(claim.Type, claim.Value, valueType: null, issuer: claim.Issuer ?? subject.Issuer));

        return new ClaimsPrincipal(identity);
    }
}
