using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using FastEndpoints;

namespace SampleGroup.Endpoints;

/// <summary>
/// The SampleGroup endpoint.
/// </summary>
/// <remarks>
/// Thin on purpose: validate, call a service, map to a DTO, respond. It owns no data access and no
/// business logic — if the same operation would be wanted from a background job or another
/// endpoint, it belongs in a service instead.
///
/// This handler is only half of the endpoint. ListSampleGroupOption is what declares the route;
/// without it this class compiles, is never registered, and the route returns 404.
/// </remarks>
[ExcludeFromCodeCoverage]
#if (IsList)
public sealed class ListSampleGroupEndpoint : EndpointWithoutRequest<List<SampleGroupSummaryDto>>
#else
public sealed class ListSampleGroupEndpoint : Endpoint<SampleGroupRequest, SampleGroupSummaryDto>
#endif
{
    /// <inheritdoc />
    public override void Configure()
    {
        // Why this verb: GET lists and single reads, POST creates, PATCH modifies, DELETE removes.
        // There is no PUT — a partial update that cannot express "not supplied" silently blanks
        // fields the caller never mentioned.
#if (IsList)
        Get("api/v1/ROUTE_PREFIX");
#elif (IsRead)
        Get("api/v1/ROUTE_PREFIX/{id}");
#elif (IsCreate)
        Post("api/v1/ROUTE_PREFIX");
#elif (IsUpdate)
        Patch("api/v1/ROUTE_PREFIX/{id}");
#elif (IsDelete)
        Delete("api/v1/ROUTE_PREFIX/{id}");
#endif
        AllowAnonymous();
        Summary(s => s.Summary = "SampleGroup");
        Tags("SampleGroup");
    }

    /// <inheritdoc />
#if (IsList)
    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        // Replace with a call to the domain service that owns this data.
        await Send.OkAsync(new List<SampleGroupSummaryDto>(), cancellationToken).ConfigureAwait(false);
    }
#else
    public override async Task HandleAsync(SampleGroupRequest req, CancellationToken cancellationToken)
    {
        // Replace with a call to the domain service that owns this data.
        await Send.NotFoundAsync(cancellationToken).ConfigureAwait(false);
    }
#endif
}
