using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.Http.Abstractions.EndPoints;
using FdwReferenceHost.Configuration;
using Microsoft.AspNetCore.Http;

namespace FdwReferenceHost.Middleware;

/// <summary>
/// Buffers response bodies into a MemoryStream so Content-Length is set explicitly,
/// preventing chunked Transfer-Encoding on responses that some clients cannot reassemble.
/// </summary>
/// <remarks>
/// See <see cref="ResponseBufferingOptions"/>. Pipeline placement matters: register this
/// middleware INSIDE <see cref="GlobalExceptionHandlerMiddleware"/> (so it can wrap the
/// exception writer too) but OUTSIDE any middleware that itself sets Content-Length.
/// </remarks>
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

    /// <summary>Buffers the response up to the cap, or streams it once it passes the cap.</summary>
    /// <param name="context">The current request.</param>
    public async Task Invoke(HttpContext context)
    {
        // Skip when disabled, on excluded paths, or for streaming/upgrade requests.
        if (!_options.Enabled || ShouldBypass(context))
        {
            await _next(context).ConfigureAwait(false);
            return;
        }

        var originalBody = context.Response.Body;
        var capped = new CappedBufferStream(originalBody, _options.MaxBufferBytes);
        context.Response.Body = capped;

        try
        {
            await _next(context).ConfigureAwait(false);

            // Why nothing is left to do once it spilled: the bytes already went to the client, so
            // the response is chunked and Content-Length can no longer be set.
            if (capped.Buffered is not { } buffered)
            {
                return;
            }

            // Why: only set Content-Length when the response permits it. RFC 9110 §6.4.1 — 1xx,
            // 204, 304 MUST NOT carry Content-Length; Kestrel throws if we try.  Also respect a
            // downstream-set Content-Length so we don't replace a known shorter value (e.g. a
            // streaming error handler already wrote its own bytes outside the buffer).
            if (!context.Response.HasStarted
                && context.Response.ContentLength is null
                && StatusAllowsBody(context.Response.StatusCode))
            {
                context.Response.ContentLength = buffered.Length;
            }

            buffered.Position = 0;
            await buffered.CopyToAsync(originalBody, context.RequestAborted).ConfigureAwait(false);
        }
        finally
        {
            context.Response.Body = originalBody;
            await capped.DisposeAsync().ConfigureAwait(false);
        }
    }

    private bool ShouldBypass(HttpContext context)
    {
        if (context.WebSockets.IsWebSocketRequest) return true;

        // Why the endpoint's own metadata and not a path prefix: a streaming route like
        // /datasets/{name}/export cannot be written as a prefix without excluding every other route
        // under /datasets/. Buffering such a response is invisible - the bytes are identical and only
        // the timing and the memory ceiling change - so the endpoint declares what it needs.
        if (context.GetEndpoint()?.Metadata.GetMetadata<IStreamingResponse>() is not null)
        {
            return true;
        }

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

    /// <summary>
    /// Holds writes in memory until they would pass the cap, then hands everything held so far to
    /// the real body and passes every later write straight through.
    /// </summary>
    /// <remarks>
    /// Why the check happens before each write rather than after the request: comparing the size
    /// once the handler returns means the whole body is already in memory, so the cap bounds
    /// nothing. Checking before each write keeps memory at or below the cap whatever the handler
    /// writes.
    /// </remarks>
    private sealed class CappedBufferStream : Stream
    {
        private readonly Stream _inner;
        private readonly int _maxBytes;
        private MemoryStream? _buffer;

        public CappedBufferStream(Stream inner, int maxBytes)
        {
            _inner = inner;
            _maxBytes = maxBytes;
            // Why a cap of zero or less buffers nothing: there is no size at which buffering is
            // allowed, so every write goes straight through.
            _buffer = maxBytes > 0 ? new MemoryStream() : null;
        }

        /// <summary>Gets the bytes held so far, or <see langword="null"/> once they spilled to the real body.</summary>
        public MemoryStream? Buffered => _buffer;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count) => Write(buffer.AsSpan(offset, count));

        // Why a synchronous write can reach the real body: past the cap there is nowhere else to
        // put the bytes. Kestrel refuses synchronous body writes unless AllowSynchronousIO is on,
        // which is the same refusal the handler would get with no buffering in front of it.
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            if (_buffer is not null && _buffer.Length + buffer.Length <= _maxBytes)
            {
                _buffer.Write(buffer);
                return;
            }

            if (_buffer is not null)
            {
                _buffer.Position = 0;
                _buffer.CopyTo(_inner);
                _buffer.Dispose();
                _buffer = null;
            }

            _inner.Write(buffer);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_buffer is not null && _buffer.Length + buffer.Length <= _maxBytes)
            {
                _buffer.Write(buffer.Span);
                return;
            }

            if (_buffer is not null)
            {
                _buffer.Position = 0;
                await _buffer.CopyToAsync(_inner, cancellationToken).ConfigureAwait(false);
                await _buffer.DisposeAsync().ConfigureAwait(false);
                _buffer = null;
            }

            await _inner.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }

        // Why a flush while buffering does nothing: flushing sends the headers, and once they are
        // sent Content-Length can no longer be set, which is the only reason this middleware exists.
        public override void Flush()
        {
            if (_buffer is null)
            {
                _inner.Flush();
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
            => _buffer is null ? _inner.FlushAsync(cancellationToken) : Task.CompletedTask;

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _buffer?.Dispose();
                _buffer = null;
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            if (_buffer is not null)
            {
                await _buffer.DisposeAsync().ConfigureAwait(false);
                _buffer = null;
            }

            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}
