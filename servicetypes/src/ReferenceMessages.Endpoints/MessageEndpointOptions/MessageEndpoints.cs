using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceMessages.Endpoints.MessageEndpointOptions;

/// <summary>The endpoints over the message resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "MessageEndpoints")]
[TypeCollection(typeof(MessageEndpointBase), typeof(IEndpointTypeOption), typeof(MessageEndpoints))]
public partial class MessageEndpoints : EndpointTypeCollectionBase<MessageEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
