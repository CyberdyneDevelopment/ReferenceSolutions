using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The endpoints over the nfl surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "NflEndpoints")]
[TypeCollection(typeof(NflEndpointBase), typeof(IEndpointTypeOption), typeof(NflEndpoints))]
public partial class NflEndpoints : EndpointTypeCollectionBase<NflEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
