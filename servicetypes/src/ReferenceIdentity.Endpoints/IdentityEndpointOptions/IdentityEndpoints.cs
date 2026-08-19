using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceIdentity.Endpoints.IdentityEndpointOptions;

/// <summary>
/// The managed-identity endpoint group.
/// </summary>
/// <remarks>
/// Two attributes doing different jobs: [TypeOption] joins this group to EndpointGroups, and
/// [TypeCollection] is what lets an option join THIS. Membership rather than parent/child because a
/// partial class cannot span assemblies.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "IdentityEndpoints")]
[TypeCollection(typeof(IdentityEndpointBase), typeof(IEndpointTypeOption), typeof(IdentityEndpoints))]
public partial class IdentityEndpoints : EndpointTypeCollectionBase<IdentityEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
