using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceConfiguration.Endpoints.ConfigurationTypeEndpointOptions;

/// <summary>The endpoints over the configuration-type resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "ConfigurationTypeEndpoints")]
[TypeCollection(typeof(ConfigurationTypeEndpointBase), typeof(IEndpointTypeOption), typeof(ConfigurationTypeEndpoints))]
public partial class ConfigurationTypeEndpoints : EndpointTypeCollectionBase<ConfigurationTypeEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
