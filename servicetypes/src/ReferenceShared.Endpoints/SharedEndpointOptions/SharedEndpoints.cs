using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceShared.Endpoints.SharedEndpointOptions;

/// <summary>The endpoints over the shared surface.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "SharedEndpoints")]
[TypeCollection(typeof(SharedEndpointBase), typeof(IEndpointTypeOption), typeof(SharedEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "SharedEndpoints")]
public partial class SharedEndpoints : EndpointTypeCollectionBase<SharedEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
