using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceSessionState.Endpoints.SessionStateEndpointOptions;

/// <summary>The endpoints over the session-state resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(SessionStateEndpointBase), typeof(IEndpointTypeOption), typeof(SessionStateEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "SessionStateEndpoints")]
public partial class SessionStateEndpoints : EndpointTypeCollectionBase<SessionStateEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
