using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using FdwReferenceEndpoints;

namespace FdwReferenceDataStores.Endpoints.DataStorePathEndpointOptions;

/// <summary>The endpoints over the data-store-path resource.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "DataStorePathEndpoints")]
[TypeCollection(typeof(DataStorePathEndpointBase), typeof(IEndpointTypeOption), typeof(DataStorePathEndpoints))]
public partial class DataStorePathEndpoints : EndpointTypeCollectionBase<DataStorePathEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();

}
