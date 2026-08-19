using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceConfiguration.Endpoints.ConfigurationCategoryEndpointOptions;

/// <summary>The endpoints over the configuration-category resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "ConfigurationCategoryEndpoints")]
[TypeCollection(typeof(ConfigurationCategoryEndpointBase), typeof(IEndpointTypeOption), typeof(ConfigurationCategoryEndpoints))]
public partial class ConfigurationCategoryEndpoints : EndpointTypeCollectionBase<ConfigurationCategoryEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
