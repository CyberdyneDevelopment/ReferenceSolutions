using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceExecution.Endpoints.WorkflowEndpointOptions;

/// <summary>The endpoints over the workflow resource.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "WorkflowEndpoints")]
[TypeCollection(typeof(WorkflowEndpointBase), typeof(IEndpointTypeOption), typeof(WorkflowEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "WorkflowEndpoints")]
public partial class WorkflowEndpoints : EndpointTypeCollectionBase<WorkflowEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
