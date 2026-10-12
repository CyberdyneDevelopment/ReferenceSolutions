using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceTransformations.Endpoints.TransformationsEndpointOptions;

/// <summary>The endpoints over the transformations surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "TransformationsEndpoints")]
[TypeCollection(typeof(TransformationsEndpointBase), typeof(IEndpointTypeOption), typeof(TransformationsEndpoints))]
public partial class TransformationsEndpoints : EndpointTypeCollectionBase<TransformationsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
