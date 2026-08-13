using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceConnections.Endpoints.ConnectionEndpointOptions;

/// <summary>
/// The endpoints over the connection resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "ConnectionEndpoints")]
[TypeCollection(typeof(ConnectionEndpointBase), typeof(IEndpointTypeOption), typeof(ConnectionEndpoints))]
public partial class ConnectionEndpoints : EndpointTypeCollectionBase<ConnectionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
