using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceSettings.Endpoints.TenantSettingEndpointOptions;

/// <summary>
/// The endpoints over the tenant-setting-override resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "TenantSettingEndpoints")]
[TypeCollection(typeof(TenantSettingEndpointBase), typeof(IEndpointTypeOption), typeof(TenantSettingEndpoints))]
public partial class TenantSettingEndpoints : EndpointTypeCollectionBase<TenantSettingEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
