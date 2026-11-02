using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceSamples.Endpoints.SampleEndpointOptions;

/// <summary>The SampleEndpoints endpoint group.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "SampleEndpoints")]
[TypeCollection(typeof(SampleEndpointBase), typeof(IEndpointTypeOption), typeof(SampleEndpoints))]
public partial class SampleEndpoints : EndpointTypeCollectionBase<SampleEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
