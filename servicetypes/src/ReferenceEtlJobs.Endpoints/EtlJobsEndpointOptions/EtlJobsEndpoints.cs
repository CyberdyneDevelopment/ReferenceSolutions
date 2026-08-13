using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Fdw.Collections.Attributes;
using Fdw.Web.RestEndpoints.EndpointTypeOptions;

namespace ReferenceEtlJobs.Endpoints.EtlJobsEndpointOptions;

/// <summary>The ETL server's jobs endpoints.</summary>
[ExcludeFromCodeCoverage]
[TypeCollection(typeof(EtlJobsEndpointBase), typeof(IEndpointTypeOption), typeof(EtlJobsEndpoints),
    TypeOption = typeof(Fdw.Web.RestEndpoints.EndpointTypeOptions.Endpoints),
    TypeOptionName = "EtlJobsEndpoints")]
public partial class EtlJobsEndpoints : EndpointTypeCollectionBase<EtlJobsEndpointBase>
{
    /// <inheritdoc />
    public override IEnumerable<IEndpointTypeOption> Members => All();
}
