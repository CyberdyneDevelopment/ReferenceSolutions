using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceAudit.Endpoints.AuditEndpointOptions;

/// <summary>The audit endpoint group: the routes that read the administrative audit trail (FDW-1586).</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "AuditEndpoints")]
[TypeCollection(typeof(AuditEndpointBase), typeof(IEndpointTypeOption), typeof(AuditEndpoints))]
public partial class AuditEndpoints : EndpointTypeCollectionBase<AuditEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
