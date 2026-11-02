using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceDataStores.Endpoints.DataStoreContainerEndpointOptions;

/// <summary>The endpoints over the data-store-container resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "DataStoreContainerEndpoints")]
[TypeCollection(typeof(DataStoreContainerEndpointBase), typeof(IEndpointTypeOption), typeof(DataStoreContainerEndpoints))]
public partial class DataStoreContainerEndpoints : EndpointTypeCollectionBase<DataStoreContainerEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
