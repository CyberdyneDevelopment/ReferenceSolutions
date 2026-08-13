using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceExecution.Endpoints.ExecutionEndpointOptions;

/// <summary>The endpoints over the execution resource.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "ExecutionEndpoints")]
[TypeCollection(typeof(ExecutionEndpointBase), typeof(IEndpointTypeOption), typeof(ExecutionEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "ExecutionEndpoints")]
public partial class ExecutionEndpoints : EndpointTypeCollectionBase<ExecutionEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
