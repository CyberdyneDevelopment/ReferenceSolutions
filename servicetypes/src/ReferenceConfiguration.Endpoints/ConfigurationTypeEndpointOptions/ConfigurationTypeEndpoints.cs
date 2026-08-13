using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceConfiguration.Endpoints.ConfigurationTypeEndpointOptions;

/// <summary>The endpoints over the configuration-type resource.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(ConfigurationTypeEndpointBase), typeof(IEndpointTypeOption), typeof(ConfigurationTypeEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "ConfigurationTypeEndpoints")]
public partial class ConfigurationTypeEndpoints : EndpointTypeCollectionBase<ConfigurationTypeEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
