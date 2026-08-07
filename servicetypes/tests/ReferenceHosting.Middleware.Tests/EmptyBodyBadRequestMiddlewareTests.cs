using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ReferenceHosting.Middleware;
using Shouldly;
using Xunit;

namespace ReferenceHosting.Middleware.Tests;

/// <summary>
/// Covers <see cref="EmptyBodyBadRequestMiddleware"/> — that a body-bearing verb arriving with no
/// body is answered 400 rather than 415, and that declared body-less routes still reach the endpoint.
/// </summary>
public class EmptyBodyBadRequestMiddlewareTests
{
    private static readonly EmptyBodyOptions Options = new()
    {
        BodylessPaths = ["/api/v1/auth/logout"],
        BodylessRoutes = [new BodylessRoute("/api/v1/pipelines/", "/execute")],
    };

    private sealed class Downstream
    {
        public bool Called { get; private set; }
        public string? Body { get; private set; }
        public string? ContentType { get; private set; }

        public async Task Invoke(HttpContext ctx)
        {
            Called = true;
            ContentType = ctx.Request.ContentType;
            using var reader = new StreamReader(ctx.Request.Body);
            Body = await reader.ReadToEndAsync();
        }
    }

    private static async Task<(HttpContext Context, Downstream Next)> Run(
        string method, string path, long? contentLength = null, string? contentType = null)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        context.Request.ContentLength = contentLength;
        context.Request.ContentType = contentType;
        context.Response.Body = new MemoryStream();

        var next = new Downstream();
        await new EmptyBodyBadRequestMiddleware(next.Invoke, Options).Invoke(context);
        return (context, next);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Hosting")]
    public async Task AnswersFourHundredWhenABodyBearingVerbArrivesWithNoBody(string method)
    {
        var (context, next) = await Run(method, "/api/v1/connections");

        context.Response.StatusCode.ShouldBe(400);
        next.Called.ShouldBeFalse();
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("DELETE")]
    [InlineData("HEAD")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Hosting")]
    public async Task LetsVerbsThatNeedNoBodyThrough(string method)
    {
        var (_, next) = await Run(method, "/api/v1/connections");

        next.Called.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Hosting")]
    public async Task DoesNotInterceptWhenABodyIsPresent()
    {
        // Why: a body in a media type the endpoint does not accept is a genuine 415, and answering
        // 400 here would hide that distinction from the caller.
        var (context, next) = await Run("POST", "/api/v1/connections", contentLength: 12, contentType: "text/plain");

        context.Response.StatusCode.ShouldNotBe(400);
        next.Called.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Hosting")]
    public async Task LetsADeclaredBodylessPathThroughWithAnEmptyJsonBody()
    {
        var (_, next) = await Run("POST", "/api/v1/auth/logout");

        next.Called.ShouldBeTrue();
        next.Body.ShouldBe("{}");
        next.ContentType.ShouldBe("application/json");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Hosting")]
    public async Task LetsADeclaredBodylessRouteShapeThrough()
    {
        var (_, next) = await Run("POST", "/api/v1/pipelines/nightly/execute");

        next.Called.ShouldBeTrue();
        next.Body.ShouldBe("{}");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Hosting")]
    public async Task DoesNotTreatAPartialRouteMatchAsBodyless()
    {
        // Why: the prefix alone is not the declaration — "/pipelines/{name}" without "/execute" is an
        // ordinary route and must still require its body.
        var (context, _) = await Run("POST", "/api/v1/pipelines/nightly");

        context.Response.StatusCode.ShouldBe(400);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Hosting")]
    public async Task MatchesDeclaredRoutesCaseInsensitively()
    {
        var (_, next) = await Run("POST", "/API/V1/Auth/Logout");

        next.Called.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Hosting")]
    public async Task WritesAStructuredFailureBody()
    {
        var (context, _) = await Run("POST", "/api/v1/connections");

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var body = await new StreamReader(context.Response.Body, Encoding.UTF8).ReadToEndAsync(TestContext.Current.CancellationToken);

        body.ShouldContain("EmptyBody");
        context.Response.ContentType.ShouldStartWith("application/json");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Hosting")]
    public void AHostThatDeclaresNothingAllowsNothing()
        => new EmptyBodyOptions().Allows("/api/v1/auth/logout").ShouldBeFalse();
}
