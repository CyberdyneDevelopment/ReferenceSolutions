using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceCalculations.Endpoints.CalculationEndpointOptions;
using ReferenceCalculations.Endpoints.CalculationEntityEndpointOptions;

namespace ReferenceCalculations.Endpoints;

/// <summary>The calculations domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "Calculations")]
public class CalculationsApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="CalculationsApiServiceType"/> class.</summary>
    public CalculationsApiServiceType()
        : base("Calculations", "Calculations", "Calculations API", "HTTP endpoints for the calculations domain.")
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
            new CalculationEndpoints(),
            new CalculationEntityEndpoints(),
        };
}
