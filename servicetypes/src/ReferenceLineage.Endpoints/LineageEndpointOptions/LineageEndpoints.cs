using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceLineage.Endpoints.LineageEndpointOptions;

/// <summary>The endpoints over the lineage surface.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(LineageEndpointBase), typeof(IEndpointTypeOption), typeof(LineageEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "LineageEndpoints")]
public partial class LineageEndpoints : EndpointTypeCollectionBase<LineageEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
