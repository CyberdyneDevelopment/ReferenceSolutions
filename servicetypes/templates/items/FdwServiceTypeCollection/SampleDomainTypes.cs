using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Services.Abstractions;

namespace SampleNamespace;

/// <summary>
/// The SampleDomain service types a host can serve.
/// </summary>
/// <remarks>
/// Declaring the collection is all this file does. A module initializer hands it to
/// <c>PlatformServices</c> under the <c>SAMPLE_CATEGORY</c> category before <c>Main()</c> runs, so
/// referencing the package that contains it IS the registration — there is no discovery step and
/// nothing to switch on.
///
/// The phase machinery is inherited and needs no code here: the generated default runs each member's
/// own phase. Add a body only when this collection itself contributes something beyond its members —
/// its domain providers. Then the body is set in a static constructor with <c>Registration(...)</c>,
/// which replaces the default, so it runs each option's Register itself.
///
/// <b>If this collection has domain providers</b>, three rules follow, and all of them were learned
/// the hard way:
///
/// 1. Add the domain's providers FIRST, then loop the options:
///    <c>foreach (var option in Options) { var r = option.Register(builder, loggerFactory); if (r.IsFailure) return r; }</c>.
///    Every option registers into, or around, the domain's registrations, so they must already exist.
///
/// 2. Match each registration to its provider's lifetime. The domain CONFIGURATION provider is a
///    singleton, so an option registers into it from Initialize — root's one call reaches the instance
///    every later resolution sees. The domain SERVICE provider is scoped, so a registration made into
///    root's instance at Initialize never reaches a request's instance, and the request fails with "No
///    factory registered for service option type 'X'". An option therefore decorates the scoped
///    registration from its own Register: it removes the domain's <c>ServiceDescriptor</c> and re-adds a
///    factory that builds the previous instance and registers itself into it. Decorations compose across
///    however many options there are. <c>NotificationServiceTypes</c> and <c>EmailNotificationType</c>
///    are both sides of this, working.
///
/// 3. Log the registry, at every level. A provider that registered zero implementations can create
///    nothing for its lifetime, so say so once, at construction; carry the registry contents on a lookup
///    miss, because "no factory for X" cannot otherwise tell an empty registry from a differently keyed
///    one, and those have opposite causes. <c>ServiceTypeLog</c> has the domain-level lines
///    (<c>DomainOptionsCollected</c>, <c>DomainProviderConstructing</c>,
///    <c>OptionFactoryRegistrationFailed</c>); <c>SampleDomainLog</c> is for what only this domain knows.
///
/// <b>The startup log is meant to be predictable.</b> Every domain emits the same shape, so a reader
/// knows what should be there and a missing line is itself the finding.
/// </remarks>
[ExcludeFromCodeCoverage]
[ServiceTypeCollection(
    typeof(SampleDomainTypeBase),
    typeof(ISampleDomainType),
    typeof(SampleDomainTypes),
    ServiceCategory = "SAMPLE_CATEGORY")]
public partial class SampleDomainTypes : ServiceTypeCollectionBase<SampleDomainTypeBase, ISampleDomainType>
{
}
