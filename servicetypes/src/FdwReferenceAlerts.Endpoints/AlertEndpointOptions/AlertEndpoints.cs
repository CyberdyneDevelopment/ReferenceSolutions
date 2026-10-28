using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceMessages.Endpoints.AlertEndpointOptions;

/// <summary>The endpoints over the alert resource: what an alert allows the caller, and taking an action on it.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "AlertEndpoints")]
[TypeCollection(typeof(AlertEndpointBase), typeof(IEndpointTypeOption), typeof(AlertEndpoints))]
public partial class AlertEndpoints : EndpointTypeCollectionBase<AlertEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
