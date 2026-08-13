using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceQuality.Endpoints.QualityRuleEndpointOptions;

/// <summary>
/// The endpoints over the quality-rule resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(QualityRuleEndpointBase), typeof(IEndpointTypeOption), typeof(QualityRuleEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "QualityRuleEndpoints")]
public partial class QualityRuleEndpoints : EndpointTypeCollectionBase<QualityRuleEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
