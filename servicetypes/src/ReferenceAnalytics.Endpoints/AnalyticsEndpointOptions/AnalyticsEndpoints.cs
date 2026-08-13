using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceAnalytics.Endpoints.AnalyticsEndpointOptions;

/// <summary>The endpoints over the analytics surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "AnalyticsEndpoints")]
[TypeCollection(typeof(AnalyticsEndpointBase), typeof(IEndpointTypeOption), typeof(AnalyticsEndpoints))]
public partial class AnalyticsEndpoints : EndpointTypeCollectionBase<AnalyticsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
