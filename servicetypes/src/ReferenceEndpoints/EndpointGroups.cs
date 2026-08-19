using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;

using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceEndpoints;

/// <summary>
/// Every group of endpoints an application serves — one member per resource.
/// </summary>
/// <remarks>
/// A group is the endpoints over one resource: ScheduleEndpoints, DataSetEndpoints and their siblings.
/// Each joins by declaring itself a TypeOption of this collection, so referencing the package that
/// holds it is what puts it here and nothing maintains a list.
///
/// Why this exists rather than the collections declaring Endpoints as a parent: the parent/child
/// arguments on the collection attributes emit a partial class onto the parent, and a partial cannot
/// span assemblies — the parent is in this package and the groups are in another. Membership of a
/// collection has no such limit, which is the same reason ApiClientTypes collects client options
/// declared across several packages.
///
/// TGeneric is the interface rather than a class because the groups share no base: each derives a
/// different closed EndpointTypeCollectionBase, so the interface is the only type they all are.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeCollection(
    typeof(IEndpointTypeCollection),
    typeof(IEndpointTypeCollection),
    typeof(EndpointGroups))]
public partial class EndpointGroups : TypeCollectionBase<IEndpointTypeCollection, IEndpointTypeCollection>
{
}
