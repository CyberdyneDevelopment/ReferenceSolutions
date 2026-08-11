using System.Collections.Generic;
using Fdw.Collections;
using Fdw.Web.RestEndpoints.ApiServiceTypeOptions;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;
using ReferenceDataStores.Endpoints.DataStoreEndpointOptions;
using ReferenceDataStores.Endpoints.DataStorePathEndpointOptions;

namespace ReferenceDataStores.Endpoints;

/// <summary>The data stores domain's API surface.</summary>
[ServiceTypeOption(typeof(Fdw.Web.RestEndpoints.ApiServiceTypeOptions.ApiServiceTypes), "DataStores")]
public class DataStoresApiServiceType : ApiServiceTypeBase
{
    /// <summary>Initializes a new instance of the <see cref="DataStoresApiServiceType"/> class.</summary>
    public DataStoresApiServiceType()
        : base("DataStores", "DataStores", "DataStores API", "HTTP endpoints for the data stores domain.")
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
            new DataStoreEndpoints(),
            new DataStorePathEndpoints(),
            new DataStoreContainerEndpoints(),
        };
}
