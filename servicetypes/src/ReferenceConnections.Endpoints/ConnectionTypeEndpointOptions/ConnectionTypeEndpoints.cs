using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceConnections.Endpoints.ConnectionTypeEndpointOptions;

/// <summary>
/// The endpoints over the connection-type resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "ConnectionTypeEndpoints")]
[TypeCollection(typeof(ConnectionTypeEndpointBase), typeof(IEndpointTypeOption), typeof(ConnectionTypeEndpoints))]
public partial class ConnectionTypeEndpoints : EndpointTypeCollectionBase<ConnectionTypeEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
