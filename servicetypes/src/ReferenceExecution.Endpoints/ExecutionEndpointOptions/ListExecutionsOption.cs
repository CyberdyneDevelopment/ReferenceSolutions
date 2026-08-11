using Fdw.Collections.Attributes;

namespace ReferenceExecution.Endpoints.ExecutionEndpointOptions;

/// <summary>The ListExecutions endpoint.</summary>
[TypeOption(typeof(ExecutionEndpoints), "ListExecutions")]
public class ListExecutionsOption : ExecutionEndpointBase<ListExecutionsEndpoint>
{
}
