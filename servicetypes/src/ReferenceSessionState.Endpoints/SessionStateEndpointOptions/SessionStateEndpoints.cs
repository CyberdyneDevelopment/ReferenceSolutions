using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceSessionState.Endpoints.SessionStateEndpointOptions;

/// <summary>The endpoints over the session-state resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "SessionStateEndpoints")]
[TypeCollection(typeof(SessionStateEndpointBase), typeof(IEndpointTypeOption), typeof(SessionStateEndpoints))]
public partial class SessionStateEndpoints : EndpointTypeCollectionBase<SessionStateEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
