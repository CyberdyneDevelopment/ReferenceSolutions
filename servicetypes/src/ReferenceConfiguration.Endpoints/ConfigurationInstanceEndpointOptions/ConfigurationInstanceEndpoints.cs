using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceConfiguration.Endpoints.ConfigurationInstanceEndpointOptions;

/// <summary>The endpoints over the configuration-instance resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.EndpointGroups), "ConfigurationInstanceEndpoints")]
[TypeCollection(typeof(ConfigurationInstanceEndpointBase), typeof(IEndpointTypeOption), typeof(ConfigurationInstanceEndpoints))]
public partial class ConfigurationInstanceEndpoints : EndpointTypeCollectionBase<ConfigurationInstanceEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
