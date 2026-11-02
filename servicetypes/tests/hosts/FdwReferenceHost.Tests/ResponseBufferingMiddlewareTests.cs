using Microsoft.AspNetCore.Http;
using FdwReferenceHost.Configuration;
using Fdw.Web.Http.Abstractions.EndPoints;
using FdwReferenceHost.Middleware;

namespace FdwReferenceHost.Tests;

/// <summary>
/// The buffer holds a response only while it fits under MaxBufferBytes, and never more than that.
/// </summary>
/// <remarks>
/// The cap used to be compared after the handler returned, by which point the whole body was
/// already in memory, so the setting bounded nothing. These tests watch the real body DURING the
/// handler's writes: a body that passes the cap must reach the client before the handler finishes.
/// </remarks>
[Trait("Category", "CoreFramework")]
public sealed class ResponseBufferingMiddlewareTests
{
    private static ResponseBufferingOptions Options(int maxBytes) => new()
    {
        Enabled = true,
        MaxBufferBytes = maxBytes,
    };

    private static DefaultHttpContext Context(Stream body)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/things";
        context.Response.Body = body;
        return context;
    }

    [Fact]
    public async Task ABodyUnderTheCapIsHeldAndSentWithContentLength()
    {
        var body = new MemoryStream();
        var context = Context(body);
        long bytesSeenByClientDuringHandler = -1;
        var middleware = new ResponseBufferingMiddleware(async ctx =>
        {
            await ctx.Response.Body.WriteAsync(new byte[10]);
            bytesSeenByClientDuringHandler = body.Length;
        }, Options(64));

        await middleware.Invoke(context);

        bytesSeenByClientDuringHandler.ShouldBe(0);
        context.Response.ContentLength.ShouldBe(10);
        body.Length.ShouldBe(10);
        context.Response.Body.ShouldBeSameAs(body);
    }

    [Fact]
    public async Task ABodyOverTheCapReachesTheClientBeforeTheHandlerFinishes()
    {
        var body = new MemoryStream();
        var context = Context(body);
        var afterFirst = -1L;
        var afterSecond = -1L;
        var middleware = new ResponseBufferingMiddleware(async ctx =>
        {
            await ctx.Response.Body.WriteAsync(new byte[40]);
            afterFirst = body.Length;
            await ctx.Response.Body.WriteAsync(new byte[40]);
            afterSecond = body.Length;
            await ctx.Response.Body.WriteAsync(new byte[40]);
        }, Options(64));

        await middleware.Invoke(context);

        afterFirst.ShouldBe(0, "40 bytes fit under a 64-byte cap and stay held");
        afterSecond.ShouldBe(80, "the second write would pass the cap, so the held bytes and it go out");
        body.Length.ShouldBe(120);
        context.Response.ContentLength.ShouldBeNull("a spilled body is chunked; its length was never known");
    }

    [Fact]
    public async Task SpilledBytesArriveInTheOrderTheyWereWritten()
    {
        var body = new MemoryStream();
        var context = Context(body);
        var middleware = new ResponseBufferingMiddleware(async ctx =>
        {
            await ctx.Response.Body.WriteAsync(new byte[] { 1, 2, 3 });
            await ctx.Response.Body.WriteAsync(new byte[] { 4, 5, 6 });
        }, Options(4));

        await middleware.Invoke(context);

        body.ToArray().ShouldBe(new byte[] { 1, 2, 3, 4, 5, 6 });
    }

    [Fact]
    public async Task ASynchronousWriterIsCappedToo()
    {
        var body = new MemoryStream();
        var context = Context(body);
        var afterSecond = -1L;
        var middleware = new ResponseBufferingMiddleware(ctx =>
        {
            ctx.Response.Body.Write(new byte[40], 0, 40);
            ctx.Response.Body.Write(new byte[40], 0, 40);
            afterSecond = body.Length;
            return Task.CompletedTask;
        }, Options(64));

        await middleware.Invoke(context);

        afterSecond.ShouldBe(80);
        body.Length.ShouldBe(80);
    }

    [Fact]
    public async Task ACapOfZeroBuffersNothing()
    {
        var body = new MemoryStream();
        var context = Context(body);
        var afterWrite = -1L;
        var middleware = new ResponseBufferingMiddleware(async ctx =>
        {
            await ctx.Response.Body.WriteAsync(new byte[5]);
            afterWrite = body.Length;
        }, Options(0));

        await middleware.Invoke(context);

        afterWrite.ShouldBe(5);
        context.Response.ContentLength.ShouldBeNull();
    }

    [Fact]
    public async Task AFlushWhileHoldingDoesNotStartTheResponse()
    {
        var body = new MemoryStream();
        var context = Context(body);
        var afterFlush = -1L;
        var middleware = new ResponseBufferingMiddleware(async ctx =>
        {
            await ctx.Response.Body.WriteAsync(new byte[8]);
            await ctx.Response.Body.FlushAsync(CancellationToken.None);
            afterFlush = body.Length;
        }, Options(64));

        await middleware.Invoke(context);

        afterFlush.ShouldBe(0);
        context.Response.ContentLength.ShouldBe(8);
    }

    [Theory]
    [InlineData(StatusCodes.Status204NoContent)]
    [InlineData(StatusCodes.Status304NotModified)]
    public async Task AStatusThatForbidsABodyGetsNoContentLength(int status)
    {
        var context = Context(new MemoryStream());
        var middleware = new ResponseBufferingMiddleware(ctx =>
        {
            ctx.Response.StatusCode = status;
            return Task.CompletedTask;
        }, Options(64));

        await middleware.Invoke(context);

        context.Response.ContentLength.ShouldBeNull();
    }

    [Fact]
    public async Task AnExcludedPathIsNeverWrapped()
    {
        var body = new MemoryStream();
        var context = Context(body);
        context.Request.Path = "/health";
        Stream? seen = null;
        var middleware = new ResponseBufferingMiddleware(ctx =>
        {
            seen = ctx.Response.Body;
            return Task.CompletedTask;
        }, Options(64));

        await middleware.Invoke(context);

        seen.ShouldBeSameAs(body);
    }

    private static DefaultHttpContext StreamingContext(Stream body)
    {
        var context = Context(body);
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(StreamingResponseMetadata.Instance),
            "streaming"));
        return context;
    }

    [Fact]
    public async Task AnEndpointDeclaringAStreamingResponseIsNeverHeld()
    {
        var body = new MemoryStream();
        var context = StreamingContext(body);
        long bytesSeenByClientDuringHandler = -1;
        var middleware = new ResponseBufferingMiddleware(async ctx =>
        {
            await ctx.Response.Body.WriteAsync(new byte[10]);
            bytesSeenByClientDuringHandler = body.Length;
        }, Options(64 * 1024));

        await middleware.Invoke(context);

        // The bytes reached the client DURING the handler. Held, this would have been 0 - and a
        // buffered streaming endpoint looks identical from the outside apart from its timing.
        bytesSeenByClientDuringHandler.ShouldBe(10);
    }

    [Fact]
    public async Task AStreamingEndpointGetsNoContentLengthSoTheResponseStaysChunked()
    {
        var body = new MemoryStream();
        var context = StreamingContext(body);
        var middleware = new ResponseBufferingMiddleware(
            async ctx => await ctx.Response.Body.WriteAsync(new byte[10]), Options(64 * 1024));

        await middleware.Invoke(context);

        context.Response.ContentLength.ShouldBeNull();
    }

    [Fact]
    public async Task TheStreamingBypassDoesNotDependOnThePath()
    {
        // The route it was needed for is /datasets/{name}/export, which no prefix can express
        // without also excluding every other route under /datasets/.
        var body = new MemoryStream();
        var context = StreamingContext(body);
        context.Request.Path = "/datasets/nfl-teams/export";
        long bytesSeenByClientDuringHandler = -1;
        var middleware = new ResponseBufferingMiddleware(async ctx =>
        {
            await ctx.Response.Body.WriteAsync(new byte[10]);
            bytesSeenByClientDuringHandler = body.Length;
        }, Options(64 * 1024));

        await middleware.Invoke(context);

        bytesSeenByClientDuringHandler.ShouldBe(10);
    }

    [Fact]
    public async Task AnEndpointWithoutTheMetadataIsStillHeld()
    {
        var body = new MemoryStream();
        var context = Context(body);
        context.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask, EndpointMetadataCollection.Empty, "ordinary"));
        long bytesSeenByClientDuringHandler = -1;
        var middleware = new ResponseBufferingMiddleware(async ctx =>
        {
            await ctx.Response.Body.WriteAsync(new byte[10]);
            bytesSeenByClientDuringHandler = body.Length;
        }, Options(64 * 1024));

        await middleware.Invoke(context);

        bytesSeenByClientDuringHandler.ShouldBe(0);
        context.Response.ContentLength.ShouldBe(10);
    }
}
