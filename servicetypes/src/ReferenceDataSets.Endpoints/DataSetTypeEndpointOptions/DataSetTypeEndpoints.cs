using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceDataSets.Endpoints.DataSetTypeEndpointOptions;

/// <summary>
/// The endpoints over the data-set-type resource.
/// </summary>
/// <remarks>
/// Not abstract, so a service type can hold this directly. The alternative was a second sealed
/// class per resource whose only content was <c>Members =&gt; All()</c> — 46 of them, existing
/// solely because an abstract collection cannot be instantiated.
/// </remarks>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "DataSetTypeEndpoints")]
[TypeCollection(typeof(DataSetTypeEndpointBase), typeof(IEndpointTypeOption), typeof(DataSetTypeEndpoints))]
public partial class DataSetTypeEndpoints : EndpointTypeCollectionBase<DataSetTypeEndpointBase>
{
    /// <inheritdoc />
    /// <remarks>
    /// Bridges the generated static <c>All()</c> to the instance surface a sweep drives through
    /// <see cref="IEndpointTypeCollection"/>.
    /// </remarks>
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
