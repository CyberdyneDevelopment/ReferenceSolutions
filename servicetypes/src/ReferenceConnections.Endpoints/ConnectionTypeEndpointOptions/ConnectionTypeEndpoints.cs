using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceConnections.Endpoints.ConnectionTypeEndpointOptions;

/// <summary>
/// The endpoints over the connection-type resource.
/// </summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "ConnectionTypeEndpoints")]
[TypeCollection(typeof(ConnectionTypeEndpointBase), typeof(IEndpointTypeOption), typeof(ConnectionTypeEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "ConnectionTypeEndpoints")]
public partial class ConnectionTypeEndpoints : EndpointTypeCollectionBase<ConnectionTypeEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
