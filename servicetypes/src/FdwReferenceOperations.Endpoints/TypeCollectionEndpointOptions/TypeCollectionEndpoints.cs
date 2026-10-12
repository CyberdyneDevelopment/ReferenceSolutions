using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceOperations.Endpoints.TypeCollectionEndpointOptions;

/// <summary>The TypeCollectionEndpoints endpoint group.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "TypeCollectionEndpoints")]
[TypeCollection(typeof(TypeCollectionEndpointBase), typeof(IEndpointTypeOption), typeof(TypeCollectionEndpoints))]
public partial class TypeCollectionEndpoints : EndpointTypeCollectionBase<TypeCollectionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
