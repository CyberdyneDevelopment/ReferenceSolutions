using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceMessages.Endpoints.AccessRequestEndpointOptions;

/// <summary>The endpoints over the access-request resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(AccessRequestEndpointBase), typeof(IEndpointTypeOption), typeof(AccessRequestEndpoints))]
public partial class AccessRequestEndpoints : EndpointTypeCollectionBase<AccessRequestEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
