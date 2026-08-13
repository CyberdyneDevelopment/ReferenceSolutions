using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceEscalation.Endpoints.EscalationEndpointOptions;

/// <summary>The endpoints over the escalation surface.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "EscalationEndpoints")]
[TypeCollection(typeof(EscalationEndpointBase), typeof(IEndpointTypeOption), typeof(EscalationEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "EscalationEndpoints")]
public partial class EscalationEndpoints : EndpointTypeCollectionBase<EscalationEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
