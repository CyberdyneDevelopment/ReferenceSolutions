using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.Clients.Abstractions;
using Microsoft.Extensions.Logging;
using SampleDomain.Clients.Models;
using System.Net.Http;

namespace SampleDomain.Clients;

/// <summary>Calls the SampleDomain surface on the API.</summary>
/// <remarks>
/// The literal below is the second of three independent declarations of this resource's route —
/// the endpoint's ResourceName is the first and the UI page directive is the third. Nothing links
/// them at compile time, so when the route moves, all three move by hand. Keeping the same value
/// this template generated is what keeps them agreeing.
/// The prefix is not repeated here: it comes from the HttpClient BaseAddress the registration sets.
/// </remarks>
[ExcludeFromCodeCoverage]
public class SampleDomainApiClient : ApiClientBase
{
    /// <summary>Initializes a new instance of the <see cref="SampleDomainApiClient"/> class.</summary>
    public SampleDomainApiClient(HttpClient httpClient, ILogger<SampleDomainApiClient> logger)
        : base(httpClient, logger)
    {
    }

    /// <summary>Lists the SampleDomain resources.</summary>
    public virtual Task<IGenericResult<IReadOnlyList<SampleDomainPayload>>> List(CancellationToken ct = default)
        => GetList<SampleDomainPayload>("sample-resource", ct);
}
