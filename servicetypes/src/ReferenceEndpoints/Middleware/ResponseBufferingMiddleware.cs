using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReferenceEndpoints.Configuration;
using Microsoft.AspNetCore.Http;

namespace ReferenceEndpoints.Middleware;

/// <summary>
/// Buffers response bodies into a MemoryStream so Content-Length is set explicitly,
/// preventing chunked Transfer-Encoding on responses that some clients cannot reassemble.
/// </summary>
/// <remarks>
/// See <see cref="ResponseBufferingOptions"/>. Pipeline placement matters: register this
/// middleware INSIDE <see cref="GlobalExceptionHandlerMiddleware"/> (so it can wrap the
/// exception writer too) but OUTSIDE any middleware that itself sets Content-Length.
/// </remarks>
[ExcludeFromCodeCoverage]
public sealed class ResponseBufferingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ResponseBufferingOptions _options;

    /// <inheritdoc />
    public ResponseBufferingMiddleware(RequestDelegate next, ResponseBufferingOptions options)
    {
        _next = next;
        _options = options ?? new ResponseBufferingOptions();
    }

    /// <inheritdoc />
    public async Task Invoke(HttpContext context)
    {
        // Skip when disabled, on excluded paths, or for streaming/upgrade requests.
        if (!_options.Enabled || ShouldBypass(context))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context).ConfigureAwait(false);

            // Why: only set Content-Length when the response permits it. RFC 9110 §6.4.1 — 1xx,
            // 204, 304 MUST NOT carry Content-Length; Kestrel throws if we try.  Also respect a
            // downstream-set Content-Length so we don't replace a known shorter value (e.g. a
            // streaming error handler already wrote its own bytes outside the buffer).
            var status = context.Response.StatusCode;
            var bodyAllowed = StatusAllowsBody(status);
            if (!context.Response.HasStarted && context.Response.ContentLength is null && bodyAllowed)
            {
                if (buffer.Length > _options.MaxBufferBytes)
                {
                    // Too large to safely buffer — fall back to chunked by NOT setting ContentLength.
                }
                else
                {
                    context.Response.ContentLength = buffer.Length;
                }
            }

            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody).ConfigureAwait(false);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private bool ShouldBypass(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest) return true;

        if (string.Equals(context.Request.Headers.Connection.ToString(),
                "Upgrade", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path)) return false;

        return _options.ExcludePathPrefixes.Any(
            prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    private static bool StatusAllowsBody(int status)
    {
        if (status >= 100 && status < 200) return false;
        if (status == 204) return false;
        if (status == 304) return false;
        return true;
    }
}
