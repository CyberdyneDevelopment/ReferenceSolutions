using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceEtl.Endpoints.Execution.ExecutionEndpointOptions;

/// <summary>The endpoints over the execution resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "ExecutionEndpoints")]
[TypeCollection(typeof(ExecutionEndpointBase), typeof(IEndpointTypeOption), typeof(ExecutionEndpoints))]
public partial class ExecutionEndpoints : EndpointTypeCollectionBase<ExecutionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
