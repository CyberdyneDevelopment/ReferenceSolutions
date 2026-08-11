using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferencePromotion.Endpoints.EnvironmentEndpointOptions;
using ReferencePromotion.Endpoints.PromotionEndpointOptions;

namespace ReferencePromotion.Endpoints;

/// <summary>
/// The promotion domain's API surface.
/// </summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Promotion")]
public class PromotionApiServiceType : ApiServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PromotionApiServiceType"/> class.
    /// </summary>
    public PromotionApiServiceType()
        : base("Promotion", "Promotion", "Promotion API", "HTTP endpoints for the promotion domain.")
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
            new PromotionEndpoints(),
            new EnvironmentEndpoints(),
        };
}
