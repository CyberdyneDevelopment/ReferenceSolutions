using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceEtlLineage.Endpoints.EtlLineageEndpointOptions;

/// <summary>The ETL server's lineage endpoints.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "EtlLineageEndpoints")]
[TypeCollection(typeof(EtlLineageEndpointBase), typeof(IEndpointTypeOption), typeof(EtlLineageEndpoints))]
public partial class EtlLineageEndpoints : EndpointTypeCollectionBase<EtlLineageEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
