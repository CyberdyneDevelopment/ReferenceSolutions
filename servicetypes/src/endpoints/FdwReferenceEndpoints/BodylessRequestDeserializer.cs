using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceEndpoints;

/// <summary>Reads a JSON request body, and treats a GET, HEAD or DELETE that carries no body as an empty request.</summary>
/// <remarks>
/// <para>
/// Why: FastEndpoints reads the body of any request that names a JSON content type, so a GET or DELETE that sends
/// <c>Content-Type: application/json</c> with nothing after it is refused 400 ("The input does not contain any JSON
/// tokens"), though there is no body to read. A client that sets the header on every call hits it on every GET (FDW-1030).
/// The request type is built empty instead, and the route and query values are bound onto it as for any request
/// without a body.
/// </para>
/// <para>
/// Only the verbs that carry no body get this. A POST, PUT or PATCH with an empty body still fails: an endpoint without a
/// validator would otherwise run on an empty request, which is what switching the validators on was meant to stop.
/// </para>
/// </remarks>
public static class BodylessRequestDeserializer
{
    /// <summary>Reads the request's body into the request type, or builds the type empty for a bodiless GET, HEAD or DELETE.</summary>
    /// <param name="request">The incoming request.</param>
    /// <param name="requestType">The type of the endpoint's request DTO.</param>
    /// <param name="jsonContext">The source-generated serializer context, when the host uses one.</param>
    /// <param name="options">The serializer options every endpoint reads with.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The request DTO.</returns>
    public static ValueTask<object?> Read(
        HttpRequest request,
        Type requestType,
        JsonSerializerContext? jsonContext,
        JsonSerializerOptions options,
        CancellationToken cancellationToken)
    {
        if (CarriesNoBody(request))
            return ValueTask.FromResult(Activator.CreateInstance(requestType));

        return jsonContext is null
            ? request.ReadFromJsonAsync(requestType, options, cancellationToken)
            : request.ReadFromJsonAsync(requestType, jsonContext, cancellationToken);
    }

    // A chunked request has no Content-Length yet still has a body, so Transfer-Encoding counts as one.
    private static bool CarriesNoBody(HttpRequest request) =>
        (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method) || HttpMethods.IsDelete(request.Method))
        && request.ContentLength is null or 0
        && request.Headers.TransferEncoding.Count == 0;
}
