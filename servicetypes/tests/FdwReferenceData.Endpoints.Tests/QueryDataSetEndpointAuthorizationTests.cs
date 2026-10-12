using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FastEndpoints;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Results;
using Fdw.Services.Data.Abstractions;
using FdwReferenceDataSets.Endpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FdwReferenceData.Endpoints.Tests;

/// <summary>
/// GET /datasets/{name}/query reads rows, so outside a DEVELOP build it answers only a caller holding
/// <c>datasets:read</c> — the FDW-876 shape its POST twin already had (FDW-910).
/// </summary>
/// <remarks>
/// A real pipeline (authentication, authorization, FastEndpoints) over an in-memory server, because the
/// defect was a route declaration, and only the pipeline reads a declaration.
/// </remarks>
[Trait("Category", "Security")]
public sealed class QueryDataSetEndpointAuthorizationTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseTestServer();
        builder.Services
            .AddAuthentication(HeaderScheme.Name)
            .AddScheme<AuthenticationSchemeOptions, HeaderScheme>(HeaderScheme.Name, _ => { });
        builder.Services.AddAuthorization(o =>
            o.AddPolicy("datasets:read", p => p.RequireClaim("perm", "datasets:read")));
        builder.Services.AddFastEndpoints(o =>
        {
            o.Assemblies = [typeof(QueryDataSetEndpoint).Assembly];
            o.DisableAutoDiscovery = true;
            o.Filter = t => t == typeof(QueryDataSetEndpoint);
        });

        // Reaching the handler is what an authorized caller should do; the data set not existing is
        // the handler's answer, and proves the request got past the gate.
        var dataSets = new Mock<IDataSetConfigurationProvider>();
        dataSets.Setup(d => d.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IDataSetImplementationConfiguration>.Success(null!));
        builder.Services.AddSingleton(dataSets.Object);
        builder.Services.AddSingleton(Mock.Of<IDataGatewayProvider>());

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFastEndpoints();
        await _app.StartAsync(TestContext.Current.CancellationToken);
        _client = _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task AnAnonymousCallerIsChallenged()
    {
        var response = await _client.GetAsync(new Uri("/datasets/Teams/query", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ACallerWithoutDatasetsReadIsForbidden()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/datasets/Teams/query", UriKind.Relative));
        request.Headers.Add(HeaderScheme.Header, "connections:read");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ACallerHoldingDatasetsReadReachesTheQuery()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/datasets/Teams/query", UriKind.Relative));
        request.Headers.Add(HeaderScheme.Header, "datasets:read");

        var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.ShouldNotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.ShouldNotBe(HttpStatusCode.Forbidden);
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
