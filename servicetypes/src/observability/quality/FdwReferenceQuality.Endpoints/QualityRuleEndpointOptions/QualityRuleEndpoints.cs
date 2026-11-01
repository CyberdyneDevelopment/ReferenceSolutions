using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceQuality.Endpoints.QualityRuleEndpointOptions;

/// <summary>
/// The endpoints over the quality-rule resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "QualityRuleEndpoints")]
[TypeCollection(typeof(QualityRuleEndpointBase), typeof(IEndpointTypeOption), typeof(QualityRuleEndpoints))]
public partial class QualityRuleEndpoints : EndpointTypeCollectionBase<QualityRuleEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
