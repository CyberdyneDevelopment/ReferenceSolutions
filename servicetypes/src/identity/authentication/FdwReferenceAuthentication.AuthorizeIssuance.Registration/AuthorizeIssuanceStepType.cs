using System.Diagnostics.CodeAnalysis;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceAuthentication.AuthorizeIssuance.Registration;

/// <summary>
/// Decides whether a token may be issued for the established principal.
/// </summary>
/// <remarks>
/// Last in a flow, because it decides on what everything before it established. The option IS the
/// step: a flow names it, the collection answers by that name, and what answers is what runs.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(AuthenticationStepTypes), "AuthorizeIssuance")]
public sealed class AuthorizeIssuanceStepType
    : AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
      IAuthenticationStep
{
    // Captured when the host is built: an option is created by its module initializer, which needs
    // a parameterless constructor, so what it needs arrives where a live container exists.
    private IIssuanceEligibility? _eligibility;
    private ILogger _logger = NullLogger<AuthorizeIssuanceStepType>.Instance;

    /// <summary>Initializes a new instance of the <see cref="AuthorizeIssuanceStepType"/> class.</summary>
    public AuthorizeIssuanceStepType()
        : base("AuthorizeIssuance",
               "AuthenticationSteps",
               "Authorize Issuance",
               "Decides whether a token may be issued for the established principal")
    {
        Initialization((host, loggerFactory) =>
        {
            _eligibility = host.Services.GetRequiredService<IIssuanceEligibility>();
            _logger = loggerFactory?.CreateLogger<AuthorizeIssuanceStepType>()
                ?? NullLogger<AuthorizeIssuanceStepType>.Instance;

            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Requires => [ContextElements.Principal];

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Contributes => [ContextElements.Decision];

    /// <inheritdoc />
    /// <remarks>Deciding is not proving. Nothing about the caller is established here.</remarks>
    public IReadOnlyList<string> AuthenticationMethods => [];

    /// <inheritdoc />
    public async Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default)
    {
        // Refusing is the only safe reading of a missing eligibility check: the alternative is
        // issuing a token because nothing was there to say no.
        if (_eligibility is null)
            return GenericResult<StepOutcome>.Failure(NotInitialized(_logger));

        var principal = context.Principal!;

        var decision = await _eligibility
            .MayBeIssued(principal, tenants, cancellationToken)
            .ConfigureAwait(false);

        if (decision.IsFailure)
            return decision.ToNewResult<StepOutcome>();

        AuthorizeIssuanceLog.EligibilityDecided(_logger, principal.Id, decision.Value!.Permitted, decision.Value!.Reason);

        return GenericResult<StepOutcome>.Success(
            new StepOutcome.Contributed(new ContextContribution { Decision = decision.Value }));
    }
}
