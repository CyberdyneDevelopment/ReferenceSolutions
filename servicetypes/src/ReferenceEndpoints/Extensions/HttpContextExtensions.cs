using Fdw.Services.Abstractions;
using ReferenceEndpoints.Middleware;
using Microsoft.AspNetCore.Http;

namespace ReferenceEndpoints.Extensions;

/// <summary>Reads and writes the ambient request context carried on the HttpContext.</summary>
public static class HttpContextExtensions
{
    private const string RequestContextKey = "FDW.RequestContext";

    /// <summary>
    /// Gets the <see cref="IRequestContext"/> for the current request.
    /// Returns <see cref="RequestContext.GuestContext"/> if not populated by middleware.
    /// </summary>
    public static IRequestContext GetRequestContext(this HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(RequestContextKey, out var value) && value is IRequestContext context)
            return context;

        return RequestContext.GuestContext;
    }

    /// <summary>
    /// Stores the <see cref="IRequestContext"/> for the current request.
    /// Called by <see cref="RequestContextMiddleware"/>.
    /// </summary>
    internal static void SetRequestContext(this HttpContext httpContext, IRequestContext context)
    {
        httpContext.Items[RequestContextKey] = context;
    }
}
