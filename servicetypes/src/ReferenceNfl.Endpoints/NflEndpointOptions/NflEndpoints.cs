using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceNfl.Endpoints.NflEndpointOptions;

/// <summary>The endpoints over the nfl surface.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "NflEndpoints")]
[TypeCollection(typeof(NflEndpointBase), typeof(IEndpointTypeOption), typeof(NflEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "NflEndpoints")]
public partial class NflEndpoints : EndpointTypeCollectionBase<NflEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
