#if (IsSeal)
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Results;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace SampleGroup.Endpoints;

/// <summary>
/// The SampleGroup endpoint: a concrete class that closes the generics of the Crud base for its operation.
/// </summary>
/// <remarks>
/// Every member the Crud base leaves abstract fails with <see cref="SampleGroupEndpointLog.NotWired"/> until you connect it to the service that owns the
/// data. ListSampleGroupOption is what declares the route; without it this class compiles, is never registered, and the route returns 404.
/// To close a base of your own instead (an open-generic base with the same members), change the base type and its using.
/// </remarks>
[ExcludeFromCodeCoverage]
#if (IsList)
public class ListSampleGroupEndpoint : CrudListEndpointBase<SampleGroupSummaryDto>
#elif (IsRead)
public class ListSampleGroupEndpoint : CrudGetEndpointBase<SampleGroupRequest, SampleGroupSummaryDto>
#elif (IsCreate)
public class ListSampleGroupEndpoint : CrudCreateEndpointBase<SampleGroupRequest, SampleGroupSummaryDto>
#elif (IsUpdate)
public class ListSampleGroupEndpoint : CrudUpdateEndpointBase<SampleGroupRequest, SampleGroupSummaryDto>
#else
public class ListSampleGroupEndpoint : CrudDeleteEndpointBase<SampleGroupRequest>
#endif
{
    /// <summary>Initializes a new instance of the <see cref="ListSampleGroupEndpoint"/> class.</summary>
    /// <param name="logger">The logger.</param>
    public ListSampleGroupEndpoint(ILogger<ListSampleGroupEndpoint> logger)
        : base(logger)
    {
    }

    /// <inheritdoc />
    protected override string ResourceName => "sample-permission";

    /// <inheritdoc />
    protected override string EndpointTag => "SampleGroup";

#if (IsList)
    /// <inheritdoc />
    protected override string EndpointSummary => "List SampleGroup";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns the SampleGroup items the caller may read.";

    /// <inheritdoc />
    protected override Task<IGenericResult<List<SampleGroupSummaryDto>>> LoadItems(CancellationToken ct)
        => Task.FromResult(GenericResult<List<SampleGroupSummaryDto>>.Failure(SampleGroupEndpointLog.NotWired(Logger, nameof(LoadItems))));
#elif (IsRead)
    /// <inheritdoc />
    protected override string EndpointSummary => "Get a SampleGroup";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns one SampleGroup item.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(SampleGroupRequest request) => request.Id.ToString();

    /// <inheritdoc />
    protected override Task<IGenericResult<SampleGroupSummaryDto?>> FindByIdentifier(SampleGroupRequest request, CancellationToken ct)
        => Task.FromResult(GenericResult<SampleGroupSummaryDto?>.Failure(SampleGroupEndpointLog.NotWired(Logger, nameof(FindByIdentifier))));
#elif (IsCreate)
    /// <inheritdoc />
    protected override string EndpointSummary => "Create a SampleGroup";

    /// <inheritdoc />
    protected override string EndpointDescription => "Creates a SampleGroup item.";

    /// <inheritdoc />
    protected override string GetResourceName(SampleGroupRequest request) => request.Id.ToString();

    /// <inheritdoc />
    protected override Task<IGenericResult<bool>> CheckExists(SampleGroupRequest request, CancellationToken ct)
        => Task.FromResult(GenericResult<bool>.Failure(SampleGroupEndpointLog.NotWired(Logger, nameof(CheckExists))));

    /// <inheritdoc />
    protected override Task<IGenericResult<SampleGroupSummaryDto>> Create(SampleGroupRequest request, CancellationToken ct)
        => Task.FromResult(GenericResult<SampleGroupSummaryDto>.Failure(SampleGroupEndpointLog.NotWired(Logger, nameof(Create))));
#elif (IsUpdate)
    /// <inheritdoc />
    protected override string EndpointSummary => "Update a SampleGroup";

    /// <inheritdoc />
    protected override string EndpointDescription => "Changes the stated fields of a SampleGroup item.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(SampleGroupRequest request) => request.Id.ToString();

    /// <inheritdoc />
    protected override Task<IGenericResult<SampleGroupSummaryDto?>> FindForUpdate(SampleGroupRequest request, CancellationToken ct)
        => Task.FromResult(GenericResult<SampleGroupSummaryDto?>.Failure(SampleGroupEndpointLog.NotWired(Logger, nameof(FindForUpdate))));

    /// <inheritdoc />
    protected override Task<IGenericResult<SampleGroupSummaryDto>> Update(SampleGroupRequest request, SampleGroupSummaryDto existing, CancellationToken ct)
        => Task.FromResult(GenericResult<SampleGroupSummaryDto>.Failure(SampleGroupEndpointLog.NotWired(Logger, nameof(Update))));
#else
    /// <inheritdoc />
    protected override string EndpointSummary => "Delete a SampleGroup";

    /// <inheritdoc />
    protected override string EndpointDescription => "Removes a SampleGroup item.";

    /// <inheritdoc />
    protected override string GetResourceIdentifier(SampleGroupRequest request) => request.Id.ToString();

    /// <inheritdoc />
    protected override Task<IGenericResult<bool>> CheckExistsForDelete(SampleGroupRequest request, CancellationToken ct)
        => Task.FromResult(GenericResult<bool>.Failure(SampleGroupEndpointLog.NotWired(Logger, nameof(CheckExistsForDelete))));

    /// <inheritdoc />
    protected override Task<IGenericResult> Delete(SampleGroupRequest request, CancellationToken ct)
        => Task.FromResult(GenericResult.Failure(SampleGroupEndpointLog.NotWired(Logger, nameof(Delete))));
#endif
}
#else
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
        // PUT is not used: it replaces the whole resource, so a partial update cannot say "not supplied"
        // and silently blanks fields the caller never mentioned. A modify is a PATCH with a nullable
        // request DTO.
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
        // Why a right is stated: a route that names none needs no sign-in, and a handler filled in
        // later would ship open. Reads gate on :read, a create or modify on :write, a delete on :delete.
#if (IsList || IsRead)
        Policies("sample-permission:read");
#elif (IsDelete)
        Policies("sample-permission:delete");
#else
        Policies("sample-permission:write");
#endif
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
#endif
