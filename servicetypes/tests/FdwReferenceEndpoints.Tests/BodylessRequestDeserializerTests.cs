using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Shouldly;
using Xunit;

namespace FdwReferenceEndpoints.Tests;

/// <summary>
/// A GET, HEAD or DELETE that names a JSON content type but carries no body is an empty request; every other
/// request is read as before (FDW-1030).
/// </summary>
public class BodylessRequestDeserializerTests
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private sealed class Probe
    {
        public string? Name { get; set; }
    }

    private static HttpRequest Request(string method, string? body)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.ContentType = "application/json";
        if (body is not null)
        {
            var bytes = Encoding.UTF8.GetBytes(body);
            context.Request.Body = new MemoryStream(bytes);
            context.Request.ContentLength = bytes.Length;
        }

        return context.Request;
    }

    private static async Task<object?> Read(HttpRequest request) =>
        await BodylessRequestDeserializer.Read(request, typeof(Probe), null, Options, default).ConfigureAwait(false);

    [Theory]
    [InlineData("GET")]
    [InlineData("HEAD")]
    [InlineData("DELETE")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Endpoints")]
    public async Task ABodilessVerbWithNoBodyIsAnEmptyRequest(string method)
    {
        var read = await Read(Request(method, null)).ConfigureAwait(false);

        read.ShouldBeOfType<Probe>().Name.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Endpoints")]
    public async Task AGetThatDoesSendABodyIsStillRead()
    {
        var read = await Read(Request("GET", "{\"name\":\"sent\"}")).ConfigureAwait(false);

        read.ShouldBeOfType<Probe>().Name.ShouldBe("sent");
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [Trait("Priority", "P0")]
    [Trait("Category", "Endpoints")]
    public async Task AVerbThatCarriesABodyStillFailsWhenItIsEmpty(string method)
    {
        await Should.ThrowAsync<JsonException>(async () => await Read(Request(method, null)).ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Endpoints")]
    public async Task AChunkedGetIsNotTreatedAsBodiless()
    {
        var request = Request("GET", "{\"name\":\"chunked\"}");
        request.ContentLength = null;
        request.Headers.TransferEncoding = "chunked";

        var read = await Read(request).ConfigureAwait(false);

        read.ShouldBeOfType<Probe>().Name.ShouldBe("chunked");
    }
}
