using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceMessages.Endpoints.AccessRequestEndpointOptions;

/// <summary>The endpoints over the access-request resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "AccessRequestEndpoints")]
[TypeCollection(typeof(AccessRequestEndpointBase), typeof(IEndpointTypeOption), typeof(AccessRequestEndpoints))]
public partial class AccessRequestEndpoints : EndpointTypeCollectionBase<AccessRequestEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
