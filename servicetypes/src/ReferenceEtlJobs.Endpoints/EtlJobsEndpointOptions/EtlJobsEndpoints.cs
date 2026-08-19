using Fdw.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;
using ReferenceEndpoints;

namespace ReferenceEtlJobs.Endpoints.EtlJobsEndpointOptions;

/// <summary>The ETL server's jobs endpoints.</summary>
[ExcludeFromCodeCoverage]
[TypeOption(typeof(EndpointGroups), "EtlJobsEndpoints")]
[TypeCollection(typeof(EtlJobsEndpointBase), typeof(IEndpointTypeOption), typeof(EtlJobsEndpoints))]
public partial class EtlJobsEndpoints : EndpointTypeCollectionBase<EtlJobsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
