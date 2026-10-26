using System.Diagnostics.CodeAnalysis;
using Fdw.Web.RestEndpoints.Crud;
using Microsoft.Extensions.Logging;

namespace SampleGroup.Endpoints;

/// <summary>
/// The open-generic base of the SampleGroup endpoint: the Crud base for the operation with the group's resource name and its OpenAPI summary,
/// description and tag stated, and its generics left open.
/// </summary>
/// <remarks>
/// A concrete class closes the generics and implements what is left abstract (the data members). Generate one with
/// <c>--Mode Seal</c>. The resource name is also the prefix of the right the Crud base gates the route on (for example <c>sample-permission:read</c>), and the
/// summary, description and tag are what the OpenAPI document shows for the route.
/// </remarks>
[ExcludeFromCodeCoverage]
#if (IsList)
public abstract class ListSampleGroupEndpointBase<TSummary> : CrudListEndpointBase<TSummary>
    where TSummary : class
#elif (IsRead)
public abstract class ListSampleGroupEndpointBase<TRequest, TDetail> : CrudGetEndpointBase<TRequest, TDetail>
    where TRequest : notnull, new()
    where TDetail : class
#elif (IsCreate)
public abstract class ListSampleGroupEndpointBase<TCreateRequest, TDetail> : CrudCreateEndpointBase<TCreateRequest, TDetail>
    where TCreateRequest : notnull, new()
    where TDetail : class
#elif (IsUpdate)
public abstract class ListSampleGroupEndpointBase<TUpdateRequest, TDetail> : CrudUpdateEndpointBase<TUpdateRequest, TDetail>
    where TUpdateRequest : notnull, new()
    where TDetail : class
#else
public abstract class ListSampleGroupEndpointBase<TRequest> : CrudDeleteEndpointBase<TRequest>
    where TRequest : notnull, new()
#endif
{
    /// <summary>Initializes the endpoint with its logger.</summary>
    /// <param name="logger">The logger.</param>
    protected ListSampleGroupEndpointBase(ILogger logger)
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
#elif (IsRead)
    /// <inheritdoc />
    protected override string EndpointSummary => "Get a SampleGroup";

    /// <inheritdoc />
    protected override string EndpointDescription => "Returns one SampleGroup item.";
#elif (IsCreate)
    /// <inheritdoc />
    protected override string EndpointSummary => "Create a SampleGroup";

    /// <inheritdoc />
    protected override string EndpointDescription => "Creates a SampleGroup item.";
#elif (IsUpdate)
    /// <inheritdoc />
    protected override string EndpointSummary => "Update a SampleGroup";

    /// <inheritdoc />
    protected override string EndpointDescription => "Changes the stated fields of a SampleGroup item.";
#else
    /// <inheritdoc />
    protected override string EndpointSummary => "Delete a SampleGroup";

    /// <inheritdoc />
    protected override string EndpointDescription => "Removes a SampleGroup item.";
#endif
}
