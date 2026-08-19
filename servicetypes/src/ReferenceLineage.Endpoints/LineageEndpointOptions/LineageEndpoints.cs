using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceLineage.Endpoints.LineageEndpointOptions;

/// <summary>The endpoints over the lineage surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "LineageEndpoints")]
[TypeCollection(typeof(LineageEndpointBase), typeof(IEndpointTypeOption), typeof(LineageEndpoints))]
public partial class LineageEndpoints : EndpointTypeCollectionBase<LineageEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
