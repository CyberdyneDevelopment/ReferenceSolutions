using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceExecution.Endpoints.WorkflowEndpointOptions;

/// <summary>The endpoints over the workflow resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "WorkflowEndpoints")]
[TypeCollection(typeof(WorkflowEndpointBase), typeof(IEndpointTypeOption), typeof(WorkflowEndpoints))]
public partial class WorkflowEndpoints : EndpointTypeCollectionBase<WorkflowEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
