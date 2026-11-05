using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceThresholds.Endpoints.ThresholdEndpointOptions;

/// <summary>The endpoints over the thresholds.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "ThresholdEndpoints")]
[TypeCollection(typeof(ThresholdEndpointBase), typeof(IEndpointTypeOption), typeof(ThresholdEndpoints))]
public partial class ThresholdEndpoints : EndpointTypeCollectionBase<ThresholdEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
