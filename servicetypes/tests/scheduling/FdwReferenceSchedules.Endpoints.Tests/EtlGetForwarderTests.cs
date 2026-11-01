using System.Net;
using System.Text;
using FdwReferenceProxy.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceSchedules.Endpoints.Tests;

/// <summary>
/// FDW-1194: the public API host reaches the ETL host's pipeline lookup through a pass-through. These tests keep what makes it
/// safe: the caller's query string reaches the ETL host as sent (a repeated <c>id</c> included), the ETL host's answer comes
/// back with its status, content type and body (a refusal keeps its code and reason), and an unreachable ETL host is told apart
/// from an answer.
/// </summary>
public sealed class EtlGetForwarderTests
{
    private const string PipelineJobClient = "PipelineJobClient";

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public HttpRequestMessage? Seen { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Seen = request;
            return Task.FromResult(respond(request));
        }
    }

    private sealed class Factory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public string? AskedFor { get; private set; }

        public HttpClient CreateClient(string name)
        {
            AskedFor = name;
            return new HttpClient(handler, disposeHandler: false) { BaseAddress = new Uri("http://etl.test/api/v1/") };
        }
    }

    private static DefaultHttpContext Context(string query)
    {
        var context = new DefaultHttpContext();
        context.Request.QueryString = new QueryString(query);
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static string Written(DefaultHttpContext context)
    {
        context.Response.Body.Position = 0;
        return new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEnd();
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body, string contentType = "application/json")
        => new(status) { Content = new StringContent(body, Encoding.UTF8, contentType) };

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task TheQueryStringReachesTheEtlHostAsTheCallerSentIt()
    {
        var handler = new Handler(_ => Json(HttpStatusCode.OK, "{}"));
        var factory = new Factory(handler);
        var context = Context("?q=brew&match=startsWith&id=a&id=b,c&skip=10&take=5&total=true");

        var relayed = await EtlGetForwarder.Forward(context, factory, NullLogger.Instance, "pipelines/lookup", TestContext.Current.CancellationToken);

        relayed.ShouldBeTrue();
        factory.AskedFor.ShouldBe(PipelineJobClient);
        handler.Seen.ShouldNotBeNull();
        handler.Seen.Method.ShouldBe(HttpMethod.Get);
        handler.Seen.RequestUri.ShouldNotBeNull();
        handler.Seen.RequestUri.PathAndQuery.ShouldBe("/api/v1/pipelines/lookup?q=brew&match=startsWith&id=a&id=b,c&skip=10&take=5&total=true");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ARequestWithoutAQueryStringAsksForTheBareRoute()
    {
        var handler = new Handler(_ => Json(HttpStatusCode.OK, "{}"));

        await EtlGetForwarder.Forward(Context(string.Empty), new Factory(handler), NullLogger.Instance, "pipelines/lookup", TestContext.Current.CancellationToken);

        handler.Seen.ShouldNotBeNull();
        handler.Seen.RequestUri.ShouldNotBeNull();
        handler.Seen.RequestUri.PathAndQuery.ShouldBe("/api/v1/pipelines/lookup");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task ARefusalComesBackWithItsStatusContentTypeCodeAndReason()
    {
        const string refusal = "{\"status\":400,\"detail\":\"'id=oops' is invalid; it must be a GUID\",\"code\":\"RESTENDPOINTS-21003\"}";
        var handler = new Handler(_ => Json(HttpStatusCode.BadRequest, refusal, "application/problem+json"));
        var context = Context("?id=oops");

        var relayed = await EtlGetForwarder.Forward(context, new Factory(handler), NullLogger.Instance, "pipelines/lookup", TestContext.Current.CancellationToken);

        relayed.ShouldBeTrue();
        context.Response.StatusCode.ShouldBe(400);
        context.Response.ContentType.ShouldNotBeNull();
        context.Response.ContentType.ShouldStartWith("application/problem+json");
        Written(context).ShouldBe(refusal);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task APageComesBackAsTheEtlHostSentIt()
    {
        const string page = "{\"skip\":0,\"take\":25,\"hasMore\":false,\"total\":1,\"items\":[{\"id\":\"01a11b6e-0000-7000-8000-000000000001\",\"name\":\"UsgsEarthquakeIngest\",\"implementation\":\"BatchCopy\"}],\"error\":null}";
        var handler = new Handler(_ => Json(HttpStatusCode.OK, page));
        var context = Context("?q=ingest&total=true");

        await EtlGetForwarder.Forward(context, new Factory(handler), NullLogger.Instance, "pipelines/lookup", TestContext.Current.CancellationToken);

        context.Response.StatusCode.ShouldBe(200);
        Written(context).ShouldBe(page);
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task AnUnreachableEtlHostIsToldApartFromAnAnswerAndNothingIsWritten()
    {
        var handler = new Handler(_ => throw new HttpRequestException("connection refused"));
        var context = Context("?q=x");

        var relayed = await EtlGetForwarder.Forward(context, new Factory(handler), NullLogger.Instance, "pipelines/lookup", TestContext.Current.CancellationToken);

        relayed.ShouldBeFalse();
        context.Response.HasStarted.ShouldBeFalse();
        Written(context).ShouldBeEmpty();
    }
}
