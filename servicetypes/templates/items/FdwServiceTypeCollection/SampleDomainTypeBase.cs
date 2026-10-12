using Fdw.Abstractions;
using Fdw.Services.Abstractions;
using Fdw.ServiceTypes;

namespace SampleNamespace;

/// <summary>
/// Option base for <see cref="SampleDomainTypes"/>. Every SAMPLE_CATEGORY option derives from this.
/// </summary>
/// <remarks>
/// <b>A base carries identity and metadata, not registration.</b> It closes
/// <see cref="ServiceTypeBase{TService, TFactory, TConfiguration}"/> and passes the category through —
/// nothing more. No FDW base contributes a phase segment for its options: each option sets its own
/// Register and Initialize bodies in its constructor, because only the option knows the providers it
/// adds and which domain registrations it joins.
///
/// If a base ever does contribute a segment, every derived option must use <c>AppendRegistration</c>:
/// <c>Registration(...)</c> replaces the whole phase body, the base's segment included, and nothing
/// reports that it happened. That silently un-registered every connection factory in this framework
/// once, while each option kept logging success.
///
/// Close the base on this domain's real service, factory and configuration contracts once they exist;
/// the generic placeholders below compile, and are what a domain with no runtime service uses.
/// </remarks>
public abstract class SampleDomainTypeBase
    : ServiceTypeBase<IGenericService, IServiceFactory<IGenericService, IServiceConfiguration>, IServiceConfiguration>,
      ISampleDomainType
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SampleDomainTypeBase"/> class.
    /// </summary>
    protected SampleDomainTypeBase(
        string name,
        string sectionName,
        string displayName,
        string description)
        : base(name, sectionName, displayName, description, category: "SAMPLE_CATEGORY")
    {
    }
}
