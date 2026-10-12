using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace SampleDomain.Endpoints.SampleDomainEndpointOptions;

/// <summary>The endpoints over the SampleDomain surface.</summary>
/// <remarks>
/// Auto-discovery is off, so this collection IS the route table: an endpoint class with no
/// <c>[TypeOption]</c> in here compiles and then answers 404. Declaring the option is what mounts it.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "SampleDomainEndpoints")]
[TypeCollection(typeof(SampleDomainEndpointBase), typeof(IEndpointTypeOption), typeof(SampleDomainEndpoints))]
public partial class SampleDomainEndpoints : EndpointTypeCollectionBase<SampleDomainEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
