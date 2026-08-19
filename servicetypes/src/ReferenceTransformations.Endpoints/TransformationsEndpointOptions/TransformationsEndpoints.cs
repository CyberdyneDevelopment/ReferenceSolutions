using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The endpoints over the transformations surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "TransformationsEndpoints")]
[TypeCollection(typeof(TransformationsEndpointBase), typeof(IEndpointTypeOption), typeof(TransformationsEndpoints))]
public partial class TransformationsEndpoints : EndpointTypeCollectionBase<TransformationsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
