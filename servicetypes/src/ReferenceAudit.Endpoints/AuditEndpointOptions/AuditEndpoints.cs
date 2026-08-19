using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceAudit.Endpoints.AuditEndpointOptions;

/// <summary>The endpoints over the audit surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "AuditEndpoints")]
[TypeCollection(typeof(AuditEndpointBase), typeof(IEndpointTypeOption), typeof(AuditEndpoints))]
public partial class AuditEndpoints : EndpointTypeCollectionBase<AuditEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
