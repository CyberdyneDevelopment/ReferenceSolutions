using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceDdl.Endpoints.DdlEndpointOptions;

/// <summary>The endpoints over the ddl surface.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "DdlEndpoints")]
[TypeCollection(typeof(DdlEndpointBase), typeof(IEndpointTypeOption), typeof(DdlEndpoints))]
public partial class DdlEndpoints : EndpointTypeCollectionBase<DdlEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
