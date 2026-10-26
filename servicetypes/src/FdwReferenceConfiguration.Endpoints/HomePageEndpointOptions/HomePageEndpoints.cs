using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceSettings.Endpoints.HomePageEndpointOptions;

/// <summary>
/// The endpoints over home pages: an organization's published page and draft, and the page that applies to the caller (FDW-2325).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "HomePageEndpoints")]
[TypeCollection(typeof(HomePageEndpointBase), typeof(IEndpointTypeOption), typeof(HomePageEndpoints))]
public partial class HomePageEndpoints : EndpointTypeCollectionBase<HomePageEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
