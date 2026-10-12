using Fdw.MessageLogging.Abstractions;
using Fdw.Messages;
using Microsoft.Extensions.Logging;

namespace SampleNamespace;

/// <summary>
/// MessageLogging for the SampleDomain registration lifecycle.
/// </summary>
/// <remarks>
/// <b>Every logged line in FDW comes from a class like this one.</b> Raw <c>ILogger.Log*</c> calls
/// are an analyzer error (FDW003), and <c>[MessageLogging]</c> both logs the message and returns it
/// as an <see cref="IGenericMessage"/>, so a failure can be logged and returned in one expression.
///
/// <b>EventIds follow RESULTCODE-CATALOG.md</b>: <c>Category = Id / 10000</c>.
/// <list type="bullet">
///   <item>cat 1 (10000–19999) — non-error lifecycle. Trace/Debug/Information live here.</item>
///   <item>cat 6 (60000–69999) — Configuration / Setup faults, the fail-fast-at-boot bucket. A
///         registration that did not happen belongs here, NOT in cat 9.</item>
///   <item>cat 9 (90000–99999) — genuinely unexpected/internal.</item>
/// </list>
/// Within a category, <c>C0000–C0999</c> is the FDW-reserved band: reuse the canonical number for a
/// meaning that already exists (60002 NotRegistered, 30000 NotFound, 60000 ConfigurationMissing).
/// Mint your own only in the <c>C1000–C9999</c> open band. The same logical meaning reuses the SAME
/// number in every package — <c>{PREFIX}-{number}</c> is what must be unique, not the number — but
/// two Ids must never collide inside one assembly. Enumerate before allocating:
/// <code>
/// grep -rhoE "EventId = [0-9]+|base\([0-9]{5}" &lt;assembly-dir&gt; --include=*.cs \
///   | grep -oE "[0-9]{5}" | sort -n | uniq
/// </code>
///
/// <b>Volume thins as severity rises.</b> Trace names every individual action; Critical fires once
/// for a condition that makes the domain inoperable. Take <c>ILogger&lt;T&gt;</c>, not bare
/// <c>ILogger</c>, so SourceContext names the type the line was written in — that is what makes a
/// base's contribution distinguishable from a derived option's.
/// </remarks>
public static partial class SampleDomainLog
{
    /// <summary>
    /// Trace: one line per option, naming the declaring type so a segment contributed by a BASE on
    /// the option's behalf is distinguishable from the option's own wiring.
    /// </summary>
    [MessageLogging(
        EventId = 11000,
        Level = LogLevel.Trace,
        Message = "{declaringType} registered '{serviceType}' for service option '{optionName}'")]
    public static partial IGenericMessage OptionRegistered(
        ILogger logger,
        string declaringType,
        string optionName,
        string serviceType);

    /// <summary>
    /// Debug: one line per option summarising what Trace listed individually.
    /// </summary>
    [MessageLogging(
        EventId = 11001,
        Level = LogLevel.Debug,
        Message = "Service option '{optionName}' completed registration ({count} service(s))")]
    public static partial IGenericMessage OptionRegistrationCompleted(
        ILogger logger,
        string optionName,
        int count);

    /// <summary>
    /// Information: the milestone a reader at default verbosity needs to confirm the domain came up
    /// with the members it expected.
    /// </summary>
    [MessageLogging(
        EventId = 11002,
        Level = LogLevel.Information,
        Message = "SampleDomain registered {count} service option(s): [{optionNames}]")]
    public static partial IGenericMessage DomainRegistered(
        ILogger logger,
        int count,
        string optionNames);

    /// <summary>
    /// Warning: the option loaded but is unusable. A real defect for that option, harmless for the
    /// rest of the domain — which is exactly what Warning means.
    /// </summary>
    [MessageLogging(
        EventId = 61000,
        Level = LogLevel.Warning,
        Message = "Service option '{optionName}' declared '{serviceType}' but it did not resolve from the container — '{optionName}' will not be usable")]
    public static partial IGenericMessage OptionDependencyUnresolvable(
        ILogger logger,
        string optionName,
        string serviceType);

    /// <summary>
    /// Error: one operation failed. Carry the state that distinguishes causes — here, what IS
    /// registered, because "no option named X" cannot otherwise tell an empty collection apart from
    /// a differently named one, and those have opposite causes.
    /// </summary>
    [MessageLogging(
        EventId = 61001,
        Level = LogLevel.Error,
        Message = "No service option '{optionName}' in SampleDomain; registered: [{registered}]")]
    public static partial IGenericMessage OptionNotFound(
        ILogger logger,
        string optionName,
        string registered);

    /// <summary>
    /// Critical: the domain cannot function for the lifetime of this instance, so every later
    /// failure is a symptom of this one line. Fires once, at construction — never per request.
    /// </summary>
    [MessageLogging(
        EventId = 61002,
        Level = LogLevel.Critical,
        Message = "SampleDomain registered NO service options — nothing in this domain can be created. Every option's registration either never ran or was discarded before the provider was built.")]
    public static partial IGenericMessage DomainRegisteredNothing(ILogger logger);
}
