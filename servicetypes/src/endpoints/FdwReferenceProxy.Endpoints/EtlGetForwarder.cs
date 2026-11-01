using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FdwReferenceProxy.Endpoints.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Forwards a GET to the ETL server with the caller's query string untouched and relays what comes back.
/// </summary>
/// <remarks>
/// <para>
/// The lookup routes (FDW-1194) take <c>q</c>, <c>match</c>, <c>id</c> (repeatable), <c>skip</c>, <c>take</c> and
/// <c>total</c>, and the ETL host validates them and answers a refusal with its own code and reason. A proxy that
/// re-read each parameter into a request class would copy the contract a second time and could drop a member, as the
/// schedule proxy once dropped the time zone. This forwards the query string as the caller sent it, so the ETL host
/// stays the one place that judges it, and <see cref="UpstreamPassThrough"/> hands its status, content type and body
/// back unchanged: a refusal reaches the caller with its code and reason, never as a generic error.
/// </para>
/// <para>
/// Same raw pass-through shape as <see cref="ListExecutionsProxyEndpoint"/>, over the same named client.
/// </para>
/// </remarks>
public static class EtlGetForwarder
{
    /// <summary>Forwards a GET to the ETL server and relays the response to the caller.</summary>
    /// <param name="httpContext">The caller's request context; its query string is forwarded as it is and the response is written to it.</param>
    /// <param name="factory">The HTTP client factory that holds the ETL server's named client.</param>
    /// <param name="logger">The logger of the calling endpoint.</param>
    /// <param name="path">The route on the ETL server, relative to its base address and without a query string.</param>
    /// <param name="cancellationToken">A token to cancel the call.</param>
    /// <returns>
    /// <see langword="true"/> when the ETL server answered and its answer was relayed; <see langword="false"/> when it could
    /// not be reached, in which case nothing was written and the caller answers 502.
    /// </returns>
    public static async Task<bool> Forward(
        HttpContext httpContext, IHttpClientFactory factory, ILogger logger, string path, CancellationToken cancellationToken = default)
    {
        var target = string.Concat(path, httpContext.Request.QueryString.Value);
        ProxyLog.ProxyRequest(logger, "GET", "Etl", target);

        var client = factory.CreateClient(EtlOrchestrationProxy.ClientName);
        HttpResponseMessage response;
        try
        {
            response = await client.GetAsync(target, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            ProxyLog.ProxyFailed(logger, "Etl", ex.Message);
            return false;
        }

        using (response)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                ProxyLog.ProxyFailed(logger, "Etl", $"status {(int)response.StatusCode}");

            await UpstreamPassThrough.Relay(httpContext, response, body, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }
}
