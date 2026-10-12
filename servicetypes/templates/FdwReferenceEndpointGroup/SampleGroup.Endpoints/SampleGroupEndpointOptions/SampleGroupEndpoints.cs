using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace SampleGroup.Endpoints.SampleGroupEndpointOptions;

/// <summary>
/// The SampleGroup endpoint group.
/// </summary>
/// <remarks>
/// Two attributes doing different jobs: <c>[TypeOption]</c> joins this group to
/// <see cref="EndpointGroups"/>, and <c>[TypeCollection]</c> is what lets an option join THIS.
/// Membership rather than parent/child, because a partial class cannot span assemblies and
/// endpoint groups routinely do.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "SampleGroupEndpoints")]
[TypeCollection(typeof(SampleGroupEndpointBase), typeof(IEndpointTypeOption), typeof(SampleGroupEndpoints))]
public partial class SampleGroupEndpoints : EndpointTypeCollectionBase<SampleGroupEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
