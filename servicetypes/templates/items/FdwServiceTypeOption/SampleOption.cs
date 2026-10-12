using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Results;
using Fdw.Services.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SampleNamespace;

/// <summary>
/// The SAMPLE_NAME member of <see cref="SampleDomainTypes"/>.
/// </summary>
/// <remarks>
/// A module initializer adds this to its collection at assembly load, so referencing the package
/// that contains it IS the registration.
///
/// The phase bodies are set in the constructor, and the setter chosen is the whole decision.
/// Registration replaces the phase body; Append runs after whatever is already chained; Prepend runs
/// before it. No FDW base contributes a segment today, so for a direct subclass Registration and Append
/// behave the same — <b>prefer Append</b> anyway, because if a base ever does contribute one,
/// Registration discards it and nothing reports that it happened. Every connection option once lost
/// its factory registration exactly that way while logging "Register completed successfully".
///
/// <b>Joining a domain.</b> If this option registers into its domain's providers, the lifetime decides
/// the phase. Into the domain's singleton configuration provider: from Initialization. Into its scoped
/// service provider: from Register, by decorating the domain's own <c>ServiceDescriptor</c> — a
/// registration made into root's scoped instance at Initialize never reaches a request's instance.
/// <c>EmailNotificationType</c> shows both.
///
/// Delete any phase this option does not need. The inherited default returns success, so an unset
/// phase costs nothing and says plainly that there is nothing to do.
/// </remarks>
[ExcludeFromCodeCoverage]
[Implementation(typeof(SampleDomainTypes), "SAMPLE_NAME")]
public sealed class SampleOption : SampleDomainTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SampleOption"/> class.
    /// </summary>
    public SampleOption()
        : base("SAMPLE_NAME", "SAMPLE_NAME", "SAMPLE_NAME", "What this option is.")
    {
        // Runs while the container is still being built, so it can only add to it. Anything needing
        // a resolved service belongs in Initialization instead.
        AppendRegistration((builder, loggerFactory) =>
        {
            builder.Services.TryAddScoped<ISampleService, SampleService>();

            // Why log, and why Trace: a registration that never ran and one that ran and was later
            // discarded are indistinguishable from a silent log — which is exactly how the defect
            // above survived several releases. Trace is the right level because this is the finest
            // grain there is: one line per option per host, saying what was registered and by whom.
            //
            // Why ILogger<SampleOption>: SourceContext then names the type the line was written in,
            // so an option's own wiring is distinguishable from a segment its BASE contributed on
            // its behalf. Read base-then-derived, the pair tells you both ran.
            // SampleDomainLog is this domain's own [MessageLogging] class. Its EventIds follow
            // RESULTCODE-CATALOG.md (Category = Id / 10000): category 1 for this non-error
            // lifecycle line, category 6 for a setup fault such as "registered nothing", category 9
            // only for the genuinely unexpected. The same logical meaning reuses the same number
            // across packages — the {PREFIX}-{number} Code is what must be unique, not the number —
            // but no two Ids may collide inside one assembly, so enumerate the assembly's Ids first.
            SampleDomainLog.OptionRegistered(
                loggerFactory?.CreateLogger<SampleOption>() ?? NullLogger<SampleOption>.Instance,
                nameof(SampleOption),
                Name,
                nameof(ISampleService));

            return GenericResult<IHostApplicationBuilder>.Success(builder);
        });

        // Runs after Build(), so this is the only phase with a real IServiceProvider — a resolved
        // service, a middleware pipeline or a mapped route can only exist here.
        AppendInitialization((host, loggerFactory) =>
        {
            return GenericResult<IHost>.Success(host);
        });
    }
}
