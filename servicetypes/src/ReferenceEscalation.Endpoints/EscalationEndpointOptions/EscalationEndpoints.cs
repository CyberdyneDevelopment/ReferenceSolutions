using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The endpoints over the escalation surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "EscalationEndpoints")]
[TypeCollection(typeof(EscalationEndpointBase), typeof(IEndpointTypeOption), typeof(EscalationEndpoints))]
public partial class EscalationEndpoints : EndpointTypeCollectionBase<EscalationEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
