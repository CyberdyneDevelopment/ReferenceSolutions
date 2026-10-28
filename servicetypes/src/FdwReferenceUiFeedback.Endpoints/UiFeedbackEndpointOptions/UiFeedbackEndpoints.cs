using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceMessages.Endpoints.UiFeedbackEndpointOptions;

/// <summary>The endpoints over the UI comment resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "UiFeedbackEndpoints")]
[TypeCollection(typeof(UiFeedbackEndpointBase), typeof(IEndpointTypeOption), typeof(UiFeedbackEndpoints))]
public partial class UiFeedbackEndpoints : EndpointTypeCollectionBase<UiFeedbackEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
