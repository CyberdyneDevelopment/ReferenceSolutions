using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceDataSets.Endpoints.DataSetEndpointOptions;
using ReferenceDataSets.Endpoints.DataSetSourceEndpointOptions;
using ReferenceDataSets.Endpoints.DataSetTypeEndpointOptions;

namespace ReferenceDataSets.Endpoints;

/// <summary>
/// The datasets domain's API surface.
/// </summary>
/// <remarks>
/// Declared here rather than in the framework because the concrete endpoint types live here — the
/// framework ships the abstract endpoint bases and the mechanism, and the host closes both.
/// </remarks>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "DataSets")]
public class DataSetsApiServiceType : ApiServiceTypeBase
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DataSetsApiServiceType"/> class.
    /// </summary>
    public DataSetsApiServiceType()
        : base("DataSets", "DataSets", "DataSets API", "HTTP endpoints for the datasets domain.")
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
            // Why an instance rather than the static collection: the sweep drives collections
            // polymorphically, and Members bridges to the generated static All().
            new DataSetEndpoints(),
            new DataSetSourceEndpoints(),
            new DataSetTypeEndpoints(),
        };
}
