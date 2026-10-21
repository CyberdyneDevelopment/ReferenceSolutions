using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FdwReferenceProxy.Endpoints.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace FdwReferenceProxy.Endpoints;

/// <summary>
/// Forwards a request of any method to the ETL server and relays its answer unchanged (FDW-1489): the path and query, the body
/// when the request has one, and the status and content back. The named client carries the caller's bearer token, so the ETL
/// server decides as that caller.
/// </summary>
/// <remarks>
/// Why a raw forwarder and not a typed client: the stored script routes belong to the ETL host alone, which is the only host
/// that holds a script extension. This host forwards them and runs nothing.
/// </remarks>
public static class EtlForwarder
{
    /// <summary>Forwards the request.</summary>
    /// <param name="httpContext">The request being served.</param>
    /// <param name="factory">Supplies the named client that reaches the ETL server.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="method">The HTTP method to forward with.</param>
    /// <param name="path">The ETL server's path, without a leading slash and without a query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>False when the ETL server could not be reached, so the caller answers 502; otherwise true, the answer relayed.</returns>
    public static Task<bool> Forward(
        HttpContext httpContext, IHttpClientFactory factory, ILogger logger, HttpMethod method, string path, CancellationToken cancellationToken = default) =>
        Forward(httpContext, factory, logger, method, path, [], cancellationToken);

    /// <summary>Forwards the request, carrying the named request headers to the ETL server as well.</summary>
    /// <param name="httpContext">The request being served.</param>
    /// <param name="factory">Supplies the named client that reaches the ETL server.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="method">The HTTP method to forward with.</param>
    /// <param name="path">The ETL server's path, without a leading slash and without a query.</param>
    /// <param name="forwardedHeaders">The names of the request headers to send on, such as the secret of a webhook call; a header the request lacks is not sent.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>False when the ETL server could not be reached, so the caller answers 502; otherwise true, the answer relayed.</returns>
    public static async Task<bool> Forward(
        HttpContext httpContext, IHttpClientFactory factory, ILogger logger, HttpMethod method, string path,
        IReadOnlyList<string> forwardedHeaders, CancellationToken cancellationToken = default)
    {
        var target = string.Concat(path, httpContext.Request.QueryString.Value);
        ProxyLog.ProxyRequest(logger, method.Method, "Etl", target);

        using var request = new HttpRequestMessage(method, target);
        foreach (var header in forwardedHeaders)
        {
            if (httpContext.Request.Headers.TryGetValue(header, out var values))
            {
                request.Headers.TryAddWithoutValidation(header, values.ToArray());
            }
        }

        if (httpContext.Request.ContentLength is > 0 || httpContext.Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8);
            var body = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            request.Content = new StringContent(body, Encoding.UTF8, "application/json");
        }

        var client = factory.CreateClient(EtlOrchestrationProxy.ClientName);
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex)
        {
            ProxyLog.ProxyFailed(logger, "Etl", ex.Message);
            return false;
        }

        using (response)
        {
            var answer = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                ProxyLog.ProxyFailed(logger, "Etl", $"status {(int)response.StatusCode}");
            }

            await UpstreamPassThrough.Relay(httpContext, response, answer, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }
}
