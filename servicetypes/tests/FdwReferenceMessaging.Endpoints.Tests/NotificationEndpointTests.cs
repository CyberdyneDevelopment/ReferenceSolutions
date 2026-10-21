using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using FastEndpoints;
using Fdw.Messages;
using Fdw.Results;
using Fdw.Services.Notifications.Abstractions;
using FdwReferenceNotifications.Endpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FdwReferenceMessaging.Endpoints.Tests;

/// <summary>
/// FDW-1238: creating a notification of a type that does not exist is a 400 that names the types that do, and a notification can be deleted.
/// </summary>
/// <remarks>
/// The real endpoints, routed as the host routes them. The notification provider is the only stand-in.
/// </remarks>
public sealed class NotificationEndpointTests : IAsyncLifetime
{
    private static readonly Guid StoredId = Guid.Parse("11111111-aaaa-4bbb-8ccc-222222222222");

    private readonly Mock<INotificationConfigurationProvider> _provider = new();
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
        {
            o.AddPolicy("notifications:write", p => p.RequireClaim("perm", "notifications:write"));
            o.AddPolicy("notifications:delete", p => p.RequireClaim("perm", "notifications:delete"));
        });
        builder.Services.AddFastEndpoints(o =>
        {
            o.Assemblies = [typeof(CreateNotificationEndpoint).Assembly];
            o.DisableAutoDiscovery = true;
            o.Filter = t => t == typeof(CreateNotificationEndpoint) || t == typeof(DeleteNotificationEndpoint);
        });

        _provider
            .Setup(p => p.Get(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<INotificationImplementationConfiguration>.Failure(Mock.Of<IGenericMessage>()));
        _provider
            .Setup(p => p.Get("Alerts", It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult<INotificationImplementationConfiguration>.Success(
                Mock.Of<INotificationImplementationConfiguration>(c => c.Id == StoredId && c.Name == "Alerts")));
        _provider
            .Setup(p => p.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenericResult.Success());
        builder.Services.AddSingleton(_provider.Object);

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
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ANotificationTypeThatDoesNotExistIsA400ThatNamesTheTypes()
    {
        var response = await Send(HttpMethod.Post, "/api/v1/notifications", new { name = "zz", isEnabled = true, notificationType = "Nonsense" });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await Body(response));
        var body = await Body(response);
        body.ShouldContain("Nonsense");
        body.ShouldContain("Console, Email, System, Teams, Webhook");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ANotificationWithNoTypeIsA400ThatNamesTheTypes()
    {
        var response = await Send(HttpMethod.Post, "/api/v1/notifications", new { name = "zz", isEnabled = true });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest, await Body(response));
        (await Body(response)).ShouldContain("Console, Email, System, Teams, Webhook");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ANotificationThatExistsIsDeletedById()
    {
        var response = await Send(HttpMethod.Delete, "/api/v1/notifications/Alerts");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent, await Body(response));
        _provider.Verify(p => p.Delete(StoredId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public async Task ANotificationThatDoesNotExistIsA404AndDeletesNothing()
    {
        var response = await Send(HttpMethod.Delete, "/api/v1/notifications/NoSuchNotification");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, await Body(response));
        _provider.Verify(p => p.Delete(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private async Task<HttpResponseMessage> Send(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        if (body is not null)
            request.Content = JsonContent.Create(body);
        request.Headers.Add(HeaderScheme.Header, "notifications:write,notifications:delete");
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
