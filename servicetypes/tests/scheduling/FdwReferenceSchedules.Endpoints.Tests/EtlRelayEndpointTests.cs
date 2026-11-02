using System.Net;
using System.Text;
using FastEndpoints;
using FdwReferenceProxy.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceSchedules.Endpoints.Tests;

/// <summary>
/// FDW-2875: reference-api relays 17 routes that only the ETL host serves. These tests pin what each relay forwards and under which
/// permission (the ones the ETL endpoints declare), how the route values fill the ETL route, and that a body and a path value reach
/// the ETL host as the caller sent them.
/// </summary>
public sealed class EtlRelayEndpointTests
{
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
        public HttpClient CreateClient(string name)
            => new(handler, disposeHandler: false) { BaseAddress = new Uri("http://etl.test/api/v1/") };
    }

    private static readonly IHttpClientFactory NoCalls = new Factory(new Handler(_ => throw new InvalidOperationException("A route table test makes no call.")));

    private static readonly (EtlRelayEndpointBase Endpoint, Http Verb, string Route, string Policy, string Upstream)[] Declared =
    [
        (new ListPipelineHistoryProxyEndpoint(NoCalls, NullLogger<ListPipelineHistoryProxyEndpoint>.Instance), Http.GET, "/etl/history", "pipelines:read", "etl/history"),
        (new GetPipelineExecutionProxyEndpoint(NoCalls, NullLogger<GetPipelineExecutionProxyEndpoint>.Instance), Http.GET, "/etl/history/{ExecutionId}", "pipelines:read", "etl/history/{ExecutionId}"),
        (new BulkPipelineStatusProxyEndpoint(NoCalls, NullLogger<BulkPipelineStatusProxyEndpoint>.Instance), Http.GET, "/etl/pipelines/status", "pipelines:read", "etl/pipelines/status"),
        (new GetPipelineHistoryProxyEndpoint(NoCalls, NullLogger<GetPipelineHistoryProxyEndpoint>.Instance), Http.GET, "/etl/pipelines/{Name}/history", "pipelines:read", "etl/pipelines/{Name}/history"),
        (new CancelEtlExecutionProxyEndpoint(NoCalls, NullLogger<CancelEtlExecutionProxyEndpoint>.Instance), Http.POST, "/etl/executions/{id}/cancel", "pipelines:execute", "executions/{id}/cancel"),
        (new PauseEtlExecutionProxyEndpoint(NoCalls, NullLogger<PauseEtlExecutionProxyEndpoint>.Instance), Http.POST, "/etl/executions/{id}/pause", "pipelines:execute", "executions/{id}/pause"),
        (new ResumeEtlExecutionProxyEndpoint(NoCalls, NullLogger<ResumeEtlExecutionProxyEndpoint>.Instance), Http.POST, "/etl/executions/{id}/resume", "pipelines:execute", "executions/{id}/resume"),
        (new ListProjectsProxyEndpoint(NoCalls, NullLogger<ListProjectsProxyEndpoint>.Instance), Http.GET, "/etl/projects", "projects:read", "projects"),
        (new CreateProjectProxyEndpoint(NoCalls, NullLogger<CreateProjectProxyEndpoint>.Instance), Http.POST, "/etl/projects", "projects:write", "projects"),
        (new GetProjectProxyEndpoint(NoCalls, NullLogger<GetProjectProxyEndpoint>.Instance), Http.GET, "/etl/projects/{id}", "projects:read", "projects/{id}"),
        (new UpdateProjectProxyEndpoint(NoCalls, NullLogger<UpdateProjectProxyEndpoint>.Instance), Http.PATCH, "/etl/projects/{id}", "projects:write", "projects/{id}"),
        (new DeleteProjectProxyEndpoint(NoCalls, NullLogger<DeleteProjectProxyEndpoint>.Instance), Http.DELETE, "/etl/projects/{id}", "projects:write", "projects/{id}"),
        (new ListOrchestrationNodesProxyEndpoint(NoCalls, NullLogger<ListOrchestrationNodesProxyEndpoint>.Instance), Http.GET, "/etl/nodes", "projects:read", "nodes"),
        (new CreateOrchestrationNodeProxyEndpoint(NoCalls, NullLogger<CreateOrchestrationNodeProxyEndpoint>.Instance), Http.POST, "/etl/nodes", "projects:write", "nodes"),
        (new GetOrchestrationNodeProxyEndpoint(NoCalls, NullLogger<GetOrchestrationNodeProxyEndpoint>.Instance), Http.GET, "/etl/nodes/{id}", "projects:read", "nodes/{id}"),
        (new UpdateOrchestrationNodeProxyEndpoint(NoCalls, NullLogger<UpdateOrchestrationNodeProxyEndpoint>.Instance), Http.PATCH, "/etl/nodes/{id}", "projects:write", "nodes/{id}"),
        (new DeleteOrchestrationNodeProxyEndpoint(NoCalls, NullLogger<DeleteOrchestrationNodeProxyEndpoint>.Instance), Http.DELETE, "/etl/nodes/{id}", "projects:write", "nodes/{id}"),
    ];

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void EachRelayDeclaresTheVerbTheRouteThePermissionAndTheUpstreamRouteOfItsEtlEndpoint()
    {
        Declared.Length.ShouldBe(17);

        foreach (var (endpoint, verb, route, policy, upstream) in Declared)
        {
            var name = endpoint.GetType().Name;
            endpoint.Relay.Verb.ShouldBe(verb, name);
            endpoint.Relay.Route.ShouldBe(route, name);
            endpoint.Relay.Policy.ShouldBe(policy, name);
            endpoint.Relay.Upstream.ShouldBe(upstream, name);
        }
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void NoTwoRelaysShareAVerbAndARouteAndEveryUpstreamPlaceholderIsASegmentOfTheRoute()
    {
        Declared.Select(d => $"{d.Verb} {d.Route}").Distinct(StringComparer.OrdinalIgnoreCase).Count().ShouldBe(Declared.Length);

        foreach (var (endpoint, _, route, _, upstream) in Declared)
        {
            var segments = System.Text.RegularExpressions.Regex.Matches(route, @"\{([^}]+)\}").Select(m => m.Groups[1].Value).ToList();
            var placeholders = System.Text.RegularExpressions.Regex.Matches(upstream, @"\{([^}]+)\}").Select(m => m.Groups[1].Value).ToList();
            placeholders.ShouldBe(segments, endpoint.GetType().Name);
        }
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void EachPlaceholderOfTheUpstreamRouteIsFilledWithItsRouteValueEscapedForAPathSegment()
    {
        var relay = new EtlRelay(Http.GET, "/a/{id}/b/{Name}", "p", "x/{id}/y/{Name}", "s", "d", "t");

        relay.UpstreamFor(name => name == "id" ? "a b" : "c/d").ShouldBe("x/a%20b/y/c%2Fd");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public void APlaceholderTheCallHasNoValueForIsRefusedAndNotFilledWithAGuess()
    {
        var relay = new EtlRelay(Http.GET, "/a/{id}", "p", "x/{other}", "s", "d", "t");

        var refusal = Should.Throw<InvalidOperationException>(() => relay.UpstreamFor(_ => null));

        refusal.Message.ShouldContain("other");
    }

    [Fact]
    [Trait("Priority", "P0")]
    [Trait("Category", "Api")]
    public async Task APostedBodyAndAPathValueReachTheEtlHostAtTheFilledRouteWithTheVerbOfTheRelay()
    {
        var cancel = Declared.Single(d => d.Endpoint is CancelEtlExecutionProxyEndpoint).Endpoint.Relay;
        string? sentBody = null;
        var handler = new Handler(request =>
        {
            sentBody = request.Content!.ReadAsStringAsync().Result;
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        });
        var bytes = Encoding.UTF8.GetBytes("{\"message\":\"stop\"}");
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.Body = new MemoryStream(bytes);
        context.Request.ContentLength = bytes.Length;
        context.Response.Body = new MemoryStream();

        var relayed = await EtlForwarder.Forward(
            context, new Factory(handler), NullLogger.Instance, cancel.Method, cancel.UpstreamFor(_ => "7c1f"), TestContext.Current.CancellationToken);

        relayed.ShouldBeTrue();
        handler.Seen.ShouldNotBeNull();
        handler.Seen.Method.ShouldBe(HttpMethod.Post);
        handler.Seen.RequestUri.ShouldNotBeNull();
        handler.Seen.RequestUri.PathAndQuery.ShouldBe("/api/v1/executions/7c1f/cancel");
        sentBody.ShouldBe("{\"message\":\"stop\"}");
        context.Response.StatusCode.ShouldBe(204);
    }
}
