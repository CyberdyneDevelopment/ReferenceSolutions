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
/// nothing more. A base sets no phase for its options (STC002 refuses one that does): each option sets
/// its own Registration and Initialization bodies in its constructor, because only the option knows the
/// providers it adds and which domain registrations it joins. Wiring every option of this domain needs
/// belongs in the collection's Register body, where the option set is already in hand.
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
