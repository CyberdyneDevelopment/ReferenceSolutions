using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceAnalytics.Endpoints.AnalyticsEndpointOptions;

namespace ReferenceAnalytics.Endpoints;

/// <summary>The analytics domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Analytics")]
public class AnalyticsApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="AnalyticsApiServiceType"/> class.</summary>
    public AnalyticsApiServiceType()
        : base("Analytics", "Analytics", "Analytics API", "HTTP endpoints for the analytics domain.")
    {
        Registration((builder, loggerFactory, dataStoreName, pathName, containerName) =>
            RegisterEndpoints(builder, loggerFactory));

        Configuration(builder => ConfigureEndpoints(builder));

        Initialization((host, loggerFactory) => InitializeEndpoints(host, loggerFactory));
    }

    /// <inheritdoc />
    public override IReadOnlyList<IEndpointTypeCollection> EndpointCollections { get; } =
        new IEndpointTypeCollection[] { new AnalyticsEndpoints() };
}
