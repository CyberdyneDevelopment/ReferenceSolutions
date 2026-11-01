using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FastEndpoints;
using Fdw.Results;
using Fdw.Services.Etl.Projects.Abstractions;
using Fdw.Services.Etl.Projects.Abstractions.Configuration;
using Fdw.Services.Etl.Projects.Validation;
using Fdw.Services.Etl.Projects.Writers;
using Fdw.Services.Pipelines.Abstractions;
using FdwReferenceEtl.Endpoints.Nodes;
using FluentValidation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FdwReferenceEtl.Endpoints.Tests;

/// <summary>
/// The node endpoints, routed as the host routes them, over the real writer and the real validator. The node provider and the
/// pipeline provider are the only stand-ins: the node provider records what was stored, the pipeline provider knows a fixed set of
/// pipelines by name.
/// </summary>
public abstract class NodeEndpointTestBase : IAsyncLifetime
{
    /// <summary>The pipelines the stand-in pipeline provider knows, by name.</summary>
    protected static readonly IReadOnlyDictionary<string, Guid> KnownPipelines = new Dictionary<string, Guid>(StringComparer.Ordinal)
    {
        ["A"] = Guid.Parse("00000000-0000-0000-0000-00000000000a"),
        ["B"] = Guid.Parse("00000000-0000-0000-0000-00000000000b"),
        ["C"] = Guid.Parse("00000000-0000-0000-0000-00000000000c")
    };

    /// <summary>The stand-in node provider.</summary>
    protected readonly Mock<IOrchestrationNodeConfigurationProvider> Provider = new();

    private WebApplication _app = null!;
    private HttpClient _client = null!;

    /// <summary>Gets the node the endpoint last stored, or null when nothing was stored.</summary>
    protected OrchestrationNodeImplementationConfiguration? Saved { get; private set; }

    /// <summary>Starts the host with the node endpoints and the writer.</summary>
    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication(HeaderScheme.Name)
            .AddScheme<AuthenticationSchemeOptions, HeaderScheme>(HeaderScheme.Name, _ => { });
        builder.Services.AddAuthorization(o =>
            o.AddPolicy("projects:write", p => p.RequireClaim("perm", "projects:write")));
        builder.Services.AddFastEndpoints(o =>
        {
            o.Assemblies = [typeof(CreateOrchestrationNodeEndpoint).Assembly];
            o.DisableAutoDiscovery = true;
            o.Filter = t => t == typeof(CreateOrchestrationNodeEndpoint) || t == typeof(GetOrchestrationNodeEndpoint);
        });

        Provider
            .Setup(p => p.Save(It.IsAny<OrchestrationNodeImplementationConfiguration>(), "OrchestrationNode", "OrchestrationNode", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<OrchestrationNodeImplementationConfiguration, string, string, string, CancellationToken>((config, _, _, _, _) =>
                Saved = config)
            .ReturnsAsync(GenericResult.Success());
        builder.Services.AddSingleton(Provider.Object);
        builder.Services.AddSingleton(PipelineProvider());
        builder.Services.AddSingleton<IValidator<OrchestrationNodeImplementationConfiguration>>(
            new OrchestrationNodeConfigurationValidator(Mock.Of<IServerPolicyDefaults>()));
        builder.Services.AddSingleton<OrchestrationNodeConfigurationWriter>();

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFastEndpoints(c => c.Endpoints.RoutePrefix = "api/v1");
        await _app.StartAsync(TestContext.Current.CancellationToken);
        _client = _app.GetTestClient();
    }

    /// <summary>Stops the host.</summary>
    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    /// <summary>Posts a node to <c>POST /nodes</c> as a caller who may write projects.</summary>
    protected async Task<HttpResponseMessage> Post(object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/nodes", UriKind.Relative))
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add(HeaderScheme.Header, "projects:write");
        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    /// <summary>Reads a response body as text, for the failure message of an assertion.</summary>
    protected static async Task<string> Body(HttpResponseMessage response)
        => await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

    /// <summary>Reads the reason a refusal gave: the <c>detail</c> of its problem body, decoded, so quotes in it are quotes.</summary>
    protected static async Task<string> Detail(HttpResponseMessage response)
    {
        using var body = System.Text.Json.JsonDocument.Parse(await Body(response));
        return body.RootElement.GetProperty("detail").GetString() ?? string.Empty;
    }

    /// <summary>Checks how many stores the stand-in node provider was asked for.</summary>
    protected void VerifyStored(Times times) =>
        Provider.Verify(
            p => p.Save(It.IsAny<OrchestrationNodeImplementationConfiguration>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            times);

    private static IPipelineConfigurationProvider PipelineProvider()
    {
        var pipelines = new Mock<IPipelineConfigurationProvider>();
        pipelines
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Failure(Mock.Of<Fdw.Messages.IGenericMessage>()));
        foreach (var (name, id) in KnownPipelines)
        {
            pipelines
                .Setup(p => p.Get(name, It.IsAny<CancellationToken>()))
                .ReturnsAsync(GenericResult<IPipelineImplementationConfiguration>.Success(
                    Mock.Of<IPipelineImplementationConfiguration>(c => c.Id == id)));
        }

        return pipelines.Object;
    }

    /// <summary>Authenticates a caller carrying the permissions named in one header; no header, no identity.</summary>
    private sealed class HeaderScheme(
        IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string Name = "Test";
        public const string Header = "X-Test-Permissions";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(Header, out var permissions))
                return Task.FromResult(AuthenticateResult.NoResult());

            var identity = new ClaimsIdentity(
                permissions.ToString().Split(',').Select(p => new Claim("perm", p.Trim())), Name);
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Name)));
        }
    }
}
