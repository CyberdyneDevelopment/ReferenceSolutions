using Fdw.Collections.Attributes;

namespace ReferenceConnections.Endpoints.ConnectionTypeEndpointOptions;

/// <summary>The ListConnectionTypes endpoint.</summary>
[TypeOption(typeof(ConnectionTypeEndpoints), "ListConnectionTypes")]
public class ListConnectionTypesOption : ConnectionTypeEndpointBase<ListConnectionTypesEndpoint>
{
}
