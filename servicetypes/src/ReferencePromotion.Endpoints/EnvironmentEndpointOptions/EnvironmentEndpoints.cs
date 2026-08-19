using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferencePromotion.Endpoints.EnvironmentEndpointOptions;

/// <summary>
/// The endpoints over the environment resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "EnvironmentEndpoints")]
[TypeCollection(typeof(EnvironmentEndpointBase), typeof(IEndpointTypeOption), typeof(EnvironmentEndpoints))]
public partial class EnvironmentEndpoints : EndpointTypeCollectionBase<EnvironmentEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
