using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>
/// The endpoints over the connection resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "ConnectionEndpoints")]
[TypeCollection(typeof(ConnectionEndpointBase), typeof(IEndpointTypeOption), typeof(ConnectionEndpoints))]
public partial class ConnectionEndpoints : EndpointTypeCollectionBase<ConnectionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
