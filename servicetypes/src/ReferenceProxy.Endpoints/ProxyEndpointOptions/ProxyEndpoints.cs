using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceProxy.Endpoints.ProxyEndpointOptions;

/// <summary>The endpoints over the proxy surface.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "ProxyEndpoints")]
[TypeCollection(typeof(ProxyEndpointBase), typeof(IEndpointTypeOption), typeof(ProxyEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "ProxyEndpoints")]
public partial class ProxyEndpoints : EndpointTypeCollectionBase<ProxyEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
