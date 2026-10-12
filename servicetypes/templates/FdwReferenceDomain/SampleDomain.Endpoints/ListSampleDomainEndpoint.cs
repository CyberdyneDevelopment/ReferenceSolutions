using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace SampleDomain.Endpoints;

/// <summary>Lists the SampleDomain resources (GET /sample-resource).</summary>
/// <remarks>
/// ResourceName is the single source the base derives everything from: the route becomes
/// <c>/sample-resource</c>, the read policy <c>sample-resource:read</c>, the OpenAPI tag and the
/// ETag container name. Change the literal and they all move together.
/// </remarks>
[ExcludeFromCodeCoverage]
public class ListSampleDomainEndpoint : CrudListEndpointBase<SampleDomainSummaryDto>
{
    /// <summary>Initializes a new instance of the <see cref="ListSampleDomainEndpoint"/> class.</summary>
    /// <param name="logger">The logger the base reports load failures through.</param>
    /// <remarks>
    /// The base takes the logger rather than resolving one, so an endpoint reports under its own
    /// type. Its two optional parameters — an ETag provider and an effective-settings provider —
    /// are left unpassed here: take them only when this resource actually serves conditional
    /// requests or reads effective settings.
    /// </remarks>
    public ListSampleDomainEndpoint(ILogger<ListSampleDomainEndpoint> logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    protected override string ResourceName => "sample-resource";

    /// <inheritdoc />
    protected override Task<IGenericResult<List<SampleDomainSummaryDto>>> LoadItems(CancellationToken ct)
    {
        // Replace with a call to this domain's service. Endpoints orchestrate — validate, call the
        // service, map to the DTO — so inject the domain's I*Service rather than reaching for a
        // gateway here.
        return Task.FromResult(
            GenericResult<List<SampleDomainSummaryDto>>.Success(new List<SampleDomainSummaryDto>()));
    }
}
