using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ReferenceEndpoints.Configuration;

namespace ReferenceEndpoints.Middleware;

/// <summary>
/// Answers a body-bearing request that arrived with no body at all with <c>400</c> rather than
/// <c>415</c>.
/// </summary>
/// <remarks>
/// <para>
/// An endpoint with a typed request DTO negotiates content type after model binding, so it cannot
/// tell "no body at all" — a caller fault, and a 400 — from "a body in a media type I do not accept",
/// which is genuinely a 415. This runs first and answers the first case.
/// </para>
/// <para>
/// Routes the host declares body-less are let through instead, with an empty JSON object injected so
/// a typed DTO still binds. Nothing is intercepted when a body IS present: a wrong media type stays a
/// 415, which is the correct answer to it.
/// </para>
/// </remarks>
public sealed class EmptyBodyBadRequestMiddleware
{
    private static readonly string[] EmptyBodyMessages = ["Request body is required for this endpoint."];

    private readonly RequestDelegate _next;
    private readonly EmptyBodyOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="EmptyBodyBadRequestMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next delegate in the pipeline.</param>
    /// <param name="options">The routes this host declares body-less.</param>
    public EmptyBodyBadRequestMiddleware(RequestDelegate next, EmptyBodyOptions options)
    {
        ArgumentNullException.ThrowIfNull(next);
        ArgumentNullException.ThrowIfNull(options);
        _next = next;
        _options = options;
    }

    /// <summary>Invokes the middleware.</summary>
    /// <param name="context">The request context.</param>
    /// <returns>A task that completes when the request has been handled.</returns>
    public async Task Invoke(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Why only these verbs: GET, DELETE and HEAD are complete without a body, so an absent body
        // says nothing about whether the request is well formed.
        var method = context.Request.Method;
        var isBodyVerb = HttpMethods.IsPost(method)
            || HttpMethods.IsPut(method)
            || HttpMethods.IsPatch(method);

        if (!isBodyVerb)
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        // Why absent counts as empty: clients routinely omit Content-Length entirely on an empty
        // body, so testing for zero alone misses the common case. Transfer-Encoding means a body is
        // still arriving, so it is not empty.
        var hasNoBody = context.Request.ContentLength is null or 0
            && string.IsNullOrEmpty(context.Request.Headers.TransferEncoding);

        if (_options.Allows(context.Request.Path.Value))
        {
            if (hasNoBody)
                SupplyEmptyJsonBody(context.Request);

            await _next(context).ConfigureAwait(false);
            return;
        }

        if (hasNoBody && string.IsNullOrEmpty(context.Request.ContentType))
        {
            await WriteEmptyBodyFailure(context).ConfigureAwait(false);
            return;
        }

        await _next(context).ConfigureAwait(false);
    }

    // Why: a declared body-less route still reaches an endpoint with a typed DTO, which requires a
    // parseable body and a content type. An empty object binds to a default request rather than
    // failing, which is what the route means when it says it needs no input.
    private static void SupplyEmptyJsonBody(HttpRequest request)
    {
        var emptyJson = Encoding.UTF8.GetBytes("{}");
        request.Body = new MemoryStream(emptyJson);
        request.ContentLength = emptyJson.Length;
        request.ContentType = "application/json";
    }

    private static Task WriteEmptyBodyFailure(HttpContext context)
    {
        context.Response.StatusCode = 400;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(new
        {
            errorCode = "EmptyBody",
            messages = EmptyBodyMessages,
        });
    }
}
