using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FastEndpoints;
using Fdw.Data;
using Fdw.Data.DataSets;
using Fdw.Data.DataSets.Abstractions;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Abstractions;
using Fdw.Services.Data.Abstractions;
using Fdw.Services.Workspaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FdwReferenceWorkspaces.Endpoints.Tests;

/// <summary>
/// FDW-1318: a variation a workspace owns can be deleted through the workspace
/// (<c>DELETE /workspaces/{WorkspaceName}/data-sets/{Name}</c>), as it is created.
/// </summary>
/// <remarks>
/// The real endpoint, routed as the host routes it. The workspace provider, the access policy and the variation provider are
/// the only stand-ins; the variation provider answers the two queries the endpoint makes, the variation by owner and name
/// and the views built on it by parent.
/// </remarks>
public sealed class DeleteDataSetVariationEndpointTests : IAsyncLifetime
{
    private const string WorkspaceName = "Sales";
    private const string VariationName = "DayAheadPrices";
    private static readonly Guid WorkspaceId = Guid.Parse("11111111-aaaa-4bbb-8ccc-222222222222");
    private static readonly Guid VariationId = Guid.Parse("33333333-aaaa-4bbb-8ccc-444444444444");

    private readonly Mock<IWorkspaceAccessPolicy> _access = new();
    private readonly Mock<IDataSetVariationConfigurationProvider> _variations = new();
    private readonly List<DataSetVariationDomainConfiguration> _owned = [];
    private readonly List<DataSetVariationDomainConfiguration> _dependents = [];
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
        builder.Services.AddAuthorization();
        builder.Services.AddFastEndpoints(o =>
        {
            o.Assemblies = [typeof(DeleteDataSetVariationEndpoint).Assembly];
            o.DisableAutoDiscovery = true;
            o.Filter = t => t == typeof(DeleteDataSetVariationEndpoint);
        });

        var workspace = Mock.Of<IWorkspaceImplementationConfiguration>(w => w.Id == WorkspaceId && w.Name == WorkspaceName);
        var workspaces = new Mock<IWorkspaceConfigurationProvider>();
        workspaces.Setup(w => w.Get(WorkspaceName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<IWorkspaceImplementationConfiguration>.Success(workspace));
        builder.Services.AddSingleton(workspaces.Object);

        _access.Setup(a => a.MayWrite(workspace, It.IsAny<CancellationToken>())).ReturnsAsync(GenericResult.Success());
        builder.Services.AddSingleton(_access.Object);

        // The variation by owner and name is a group of two conditions; the views built on it are one condition on the parent.
        _variations
            .Setup(v => v.Get(It.IsAny<DomainQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((DomainQuery query, CancellationToken _) =>
                GenericResult<ConfigurationPage<DataSetVariationDomainConfiguration>>.Success(
                    Page(query.Filter is FilterExpression { Root: FilterGroup } ? _owned : _dependents)));
        _variations
            .Setup(v => v.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        builder.Services.AddSingleton(_variations.Object);

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.UseFastEndpoints(c => c.Endpoints.RoutePrefix = "api/v1");
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
    [Trait("Priority", "P0")]
    public async Task AVariationTheWorkspaceOwnsIsDeletedById()
    {
        _owned.Add(Variation(VariationId, VariationName));

        var response = await Delete(VariationName);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await Body(response));
        _variations.Verify(v => v.Delete(VariationId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AVariationTheWorkspaceDoesNotOwnAnswersNotFoundAndDeletesNothing()
    {
        var response = await Delete(VariationName);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, await Body(response));
        _variations.Verify(v => v.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task AVariationWithAViewBuiltOnItIsRefusedWithAConflictAndIsNotDeleted()
    {
        _owned.Add(Variation(VariationId, VariationName));
        _dependents.Add(Variation(Guid.NewGuid(), "WorkspaceHigh"));

        var response = await Delete(VariationName);

        // The response carries the result code; the view in the way is in the refusal's logged details, not the body.
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict, await Body(response));
        (await Body(response)).ShouldContain("Workspaces-41000");
        _variations.Verify(v => v.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    [Trait("Priority", "P0")]
    public async Task ACallerWhoMayNotWriteTheWorkspaceIsRefusedWhetherOrNotTheVariationExists()
    {
        var workspaces = _app.Services.GetRequiredService<IWorkspaceConfigurationProvider>();
        var workspace = (await workspaces.Get(WorkspaceName, TestContext.Current.CancellationToken)).Value!;
        _access.Setup(a => a.MayWrite(workspace, It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Failure(Mock.Of<IGenericMessage>()));

        _owned.Add(Variation(VariationId, VariationName));
        var existing = await Delete(VariationName);
        var absent = await Delete("NoSuchView");

        existing.IsSuccessStatusCode.ShouldBeFalse();
        absent.StatusCode.ShouldBe(existing.StatusCode);
        _variations.Verify(v => v.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static DataSetVariationDomainConfiguration Variation(Guid id, string name) => new()
    {
        Id = id,
        Name = $"{WorkspaceName}/{name}",
        VariationName = name,
        OwnerWorkspaceId = WorkspaceId,
    };

    private static ConfigurationPage<DataSetVariationDomainConfiguration> Page(List<DataSetVariationDomainConfiguration> rows)
        => new(0, rows.Count, false, _ => Rows(rows.ToList()));

    private static async IAsyncEnumerable<DataSetVariationDomainConfiguration> Rows(List<DataSetVariationDomainConfiguration> rows)
    {
        foreach (var row in rows)
            yield return row;

        await Task.CompletedTask;
    }

    private async Task<HttpResponseMessage> Delete(string name)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Delete, new Uri($"/api/v1/workspaces/{WorkspaceName}/data-sets/{name}", UriKind.Relative));
        request.Headers.Add(HeaderScheme.Header, "signed-in");
        return await _client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<string> Body(HttpResponseMessage response)
        => await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

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
