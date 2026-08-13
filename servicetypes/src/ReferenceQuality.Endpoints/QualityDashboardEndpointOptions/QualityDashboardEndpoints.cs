using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceQuality.Endpoints.QualityDashboardEndpointOptions;

/// <summary>
/// The endpoints over the quality-dashboard resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "QualityDashboardEndpoints")]
[TypeCollection(typeof(QualityDashboardEndpointBase), typeof(IEndpointTypeOption), typeof(QualityDashboardEndpoints))]
public partial class QualityDashboardEndpoints : EndpointTypeCollectionBase<QualityDashboardEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
