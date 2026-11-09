using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceSettings.Endpoints.RoleSettingEndpointOptions;

/// <summary>
/// The endpoints over the role-setting-override resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "RoleSettingEndpoints")]
[TypeCollection(typeof(RoleSettingEndpointBase), typeof(IEndpointTypeOption), typeof(RoleSettingEndpoints))]
public partial class RoleSettingEndpoints : EndpointTypeCollectionBase<RoleSettingEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
