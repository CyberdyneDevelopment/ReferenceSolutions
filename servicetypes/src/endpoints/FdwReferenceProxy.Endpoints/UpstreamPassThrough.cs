using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Relays an upstream HTTP response to the caller unchanged.
/// </summary>
/// <remarks>
/// Why this exists (FDW-899): the raw-HttpClient pass-throughs answered with
/// <c>ContentType ?? "application/json"</c>, which labelled a body the upstream never labelled. A
/// pass-through relays what came back: the status, the body, and a Content-Type only when the upstream
/// sent one.
/// </remarks>
internal static class UpstreamPassThrough
{
    /// <summary>Writes the upstream status, Content-Type (when present) and body to the caller.</summary>
    /// <param name="httpContext">The caller's request context.</param>
    /// <param name="response">The upstream response.</param>
    /// <param name="body">The upstream body, already read.</param>
    /// <param name="cancellationToken">A token to cancel the write.</param>
    /// <returns>A task that completes when the body has been written.</returns>
    internal static async Task Relay(
        HttpContext httpContext, HttpResponseMessage response, string body, CancellationToken cancellationToken = default)
    {
        httpContext.Response.StatusCode = (int)response.StatusCode;
        if (response.Content.Headers.ContentType is { } contentType)
            httpContext.Response.ContentType = contentType.ToString();

        // Why StartAsync first: an empty upstream body writes nothing, and a response that has not
        // started would let the endpoint pipeline send its own default in place of the relayed status.
        await httpContext.Response.StartAsync(cancellationToken).ConfigureAwait(false);
        await httpContext.Response.WriteAsync(body, cancellationToken).ConfigureAwait(false);
    }
}
