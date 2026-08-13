using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceExecution.Endpoints.ExecutionEndpointOptions;

/// <summary>The endpoints over the execution resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "ExecutionEndpoints")]
[TypeCollection(typeof(ExecutionEndpointBase), typeof(IEndpointTypeOption), typeof(ExecutionEndpoints))]
public partial class ExecutionEndpoints : EndpointTypeCollectionBase<ExecutionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
