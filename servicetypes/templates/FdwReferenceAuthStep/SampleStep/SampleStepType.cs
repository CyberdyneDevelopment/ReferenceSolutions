using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Abstractions;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Authentication.Abstractions.Context;
using Fdw.Services.Authentication.Abstractions.Steps;
using Fdw.Services.Authentication.Flow;
using Fdw.Services.Multitenancy.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SampleStep;

/// <summary>
/// Declares this step to the platform, and is the step.
/// </summary>
/// <remarks>
/// <para>
/// The <c>[Implementation]</c> attribute is the whole registration. A module initializer attaches
/// it at assembly load, so referencing the package is what makes the step selectable — there is no
/// registry to edit and no switch to extend. Remove the reference and every flow naming this step
/// fails at startup, rather than quietly doing less than it did before.
/// </para>
/// <para>
/// The option IS the step: it implements <see cref="IAuthenticationStep"/> directly rather than
/// registering a separate service class for the runner to look up by name. What this step needs is
/// captured in <c>Initialization</c>, because an option is constructed by its module initializer —
/// which needs a parameterless constructor — so any dependency has to arrive after the host is
/// built, where a live container exists.
/// </para>
/// </remarks>
[Implementation(typeof(AuthenticationStepTypes), "SampleStep")]
public sealed class SampleStepType
    : AuthenticationStepTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>>,
      IAuthenticationStep
{
    // Captured in Initialization; see the class remarks for why this can't be a constructor parameter.
    private SampleStepConfiguration? _configuration;
    private ILogger _logger = NullLogger<SampleStepType>.Instance;

    /// <summary>Initializes a new instance of the <see cref="SampleStepType"/> class.</summary>
    public SampleStepType()
        : base(
            name: "SampleStep",
            sectionName: "SampleStep",
            displayName: "Sample step",
            description: "Replace this with what the step actually proves or resolves.")
    {
        Initialization((host, loggerFactory) =>
        {
            _configuration = host.Services.GetRequiredService<SampleStepConfiguration>();
            _logger = loggerFactory?.CreateLogger<SampleStepType>() ?? NullLogger<SampleStepType>.Instance;

            return GenericResult<IHost>.Success(host);
        });
    }

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Requires => [];

    /// <inheritdoc />
    public IReadOnlyList<IContextElement> Contributes => [ContextElements.CONTRIBUTES_ELEMENT];

    /// <inheritdoc />
    /// <remarks>
    /// A ceiling on what this step may assert, checked once the step has actually succeeded — never
    /// a claim made up front. Empty if this step proves nothing (it resolves or decides instead).
    /// </remarks>
    public IReadOnlyList<string> AuthenticationMethods => ["AMR_VALUE"];

    /// <inheritdoc />
    public Task<IGenericResult<StepOutcome>> Execute(
        AuthenticationContext context, ITenantProvider tenants, CancellationToken cancellationToken = default)
    {
        // An option whose Initialize never ran has nothing to work from, and proceeding anyway would
        // hide that the host is missing a registration this step depends on.
        if (_configuration is not { } configuration)
            return Task.FromResult(GenericResult<StepOutcome>.Failure(NotInitialized(_logger)));

        // A step that must send the caller somewhere returns a Challenge instead, and the runner
        // persists what has been established until something resumes it:
        //
        //   return Ok(new StepOutcome.Challenge(destination, resumeToken));
        //
        // A step with nothing to do for this flow returns NotApplicable, which is not a failure —
        // but it contributes nothing, so a later step requiring that contribution still fails.

        SampleStepLog.Executed(_logger, configuration.Example);

        return Task.FromResult(GenericResult<StepOutcome>.Success(
            new StepOutcome.Contributed(new ContextContribution
            {
                // Populate exactly what Contributes declares. Anything else is dropped.
            })));
    }
}
