using Fdw.Collections.Attributes;

namespace SampleDomain.Endpoints.SampleDomainEndpointOptions;

/// <summary>Declares the list endpoint into this group.</summary>
[TypeOption(typeof(SampleDomainEndpoints), "ListSampleDomain")]
public class ListSampleDomainOption : SampleDomainEndpointBase<ListSampleDomainEndpoint>
{
}
