using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceSettings.Endpoints.ServerSettingEndpointOptions;

/// <summary>
/// The endpoints over the server-setting resource.
/// </summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "ServerSettingEndpoints")]
[TypeCollection(typeof(ServerSettingEndpointBase), typeof(IEndpointTypeOption), typeof(ServerSettingEndpoints))]
public partial class ServerSettingEndpoints : EndpointTypeCollectionBase<ServerSettingEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
