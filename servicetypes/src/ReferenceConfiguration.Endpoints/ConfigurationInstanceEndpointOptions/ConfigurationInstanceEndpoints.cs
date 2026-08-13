using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceConfiguration.Endpoints.ConfigurationInstanceEndpointOptions;

/// <summary>The endpoints over the configuration-instance resource.</summary>
[ExcludeFromCodeCoverage]
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints), "ConfigurationInstanceEndpoints")]
[TypeCollection(typeof(ConfigurationInstanceEndpointBase), typeof(IEndpointTypeOption), typeof(ConfigurationInstanceEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "ConfigurationInstanceEndpoints")]
public partial class ConfigurationInstanceEndpoints : EndpointTypeCollectionBase<ConfigurationInstanceEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
