using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceConfiguration.Endpoints.ConfigurationCategoryEndpointOptions;

/// <summary>The endpoints over the configuration-category resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ConfigurationCategoryEndpointBase), typeof(IEndpointTypeOption), typeof(ConfigurationCategoryEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "ConfigurationCategoryEndpoints")]
public partial class ConfigurationCategoryEndpoints : EndpointTypeCollectionBase<ConfigurationCategoryEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
