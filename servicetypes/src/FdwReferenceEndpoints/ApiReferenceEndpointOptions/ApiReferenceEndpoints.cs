using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace FdwReferenceEndpoints.ApiReferenceEndpointOptions;

/// <summary>
/// The API reference endpoints every host serves: the whole service surface cut to the caller, and
/// the list of reference pages the caller may open.
/// </summary>
/// <remarks>
/// Declared here, beside the pipeline that maps the reference pages, so every host that composes this
/// surface serves its own reference. The sign-in and data references are declared by the auth and
/// workspace packages, which only the hosts that serve those routes reference.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "ApiReferenceEndpoints")]
[TypeCollection(typeof(ApiReferenceEndpointBase), typeof(IEndpointTypeOption), typeof(ApiReferenceEndpoints))]
public partial class ApiReferenceEndpoints : EndpointTypeCollectionBase<ApiReferenceEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
