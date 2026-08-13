using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceQuality.Endpoints.QualityCheckEndpointOptions;

/// <summary>
/// The endpoints over the quality-check resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(QualityCheckEndpointBase), typeof(IEndpointTypeOption), typeof(QualityCheckEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "QualityCheckEndpoints")]
public partial class QualityCheckEndpoints : EndpointTypeCollectionBase<QualityCheckEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
