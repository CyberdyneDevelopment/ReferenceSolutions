using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceQuality.Endpoints.QualityDashboardEndpointOptions;

/// <summary>
/// The endpoints over the quality-dashboard resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(QualityDashboardEndpointBase), typeof(IEndpointTypeOption), typeof(QualityDashboardEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "QualityDashboardEndpoints")]
public partial class QualityDashboardEndpoints : EndpointTypeCollectionBase<QualityDashboardEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
