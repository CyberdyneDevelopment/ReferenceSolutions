using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceAudit.Endpoints.AuditEndpointOptions;

/// <summary>The endpoints over the audit surface.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "AuditEndpoints")]
[TypeCollection(typeof(AuditEndpointBase), typeof(IEndpointTypeOption), typeof(AuditEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "AuditEndpoints")]
public partial class AuditEndpoints : EndpointTypeCollectionBase<AuditEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
