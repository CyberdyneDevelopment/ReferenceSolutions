using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceExecution.Endpoints.ExecutionEndpointOptions;

/// <summary>The endpoints over the execution resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ExecutionEndpointBase), typeof(IEndpointTypeOption), typeof(ExecutionEndpoints))]
public partial class ExecutionEndpoints : EndpointTypeCollectionBase<ExecutionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
