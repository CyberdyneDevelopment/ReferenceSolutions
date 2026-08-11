using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceQuality.Endpoints.QualityCheckEndpointOptions;
using ReferenceQuality.Endpoints.QualityDashboardEndpointOptions;
using ReferenceQuality.Endpoints.QualityRuleEndpointOptions;

namespace ReferenceQuality.Endpoints;

/// <summary>
/// The quality domain's API surface.
/// </summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Quality")]
public class QualityApiServiceType : ApiServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QualityApiServiceType"/> class.
    /// </summary>
    public QualityApiServiceType()
        : base("Quality", "Quality", "Quality API", "HTTP endpoints for the quality domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[]
        {
            new QualityRuleEndpoints(),
            new QualityCheckEndpoints(),
            new QualityDashboardEndpoints(),
        };
}
