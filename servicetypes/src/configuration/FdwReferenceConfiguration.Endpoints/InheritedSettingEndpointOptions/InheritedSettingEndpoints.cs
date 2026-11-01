using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceSettings.Endpoints.InheritedSettingEndpointOptions;

/// <summary>
/// The endpoints over settings that inherit down the platform, tenant and organization tree and can be locked from above (FDW-1579).
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "InheritedSettingEndpoints")]
[TypeCollection(typeof(InheritedSettingEndpointBase), typeof(IEndpointTypeOption), typeof(InheritedSettingEndpoints))]
public partial class InheritedSettingEndpoints : EndpointTypeCollectionBase<InheritedSettingEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
