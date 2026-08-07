using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fdw.Web.Http.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Reference.Ui.Tests.Infrastructure;

/// <summary>
/// Registers the minimal services that real FDW providers/pages require via <c>[Inject]</c>
/// (an <see cref="IHttpClientFactory"/>, an <see cref="ILoggerFactory"/>, and an
/// <see cref="IAccessTokenProvider"/>), so that an inheriting provider stub — or a packaged page
/// that captures its provider via a typed <c>@ref</c> and so cannot be swapped — can be constructed
/// by the bUnit renderer without failing on unresolved injected dependencies.
/// </summary>
/// <remarks>
/// The HTTP client is backed by a no-op handler that returns an empty JSON array; the access token
/// provider returns <c>null</c> (no request is ever actually sent, so no real token is needed).
/// </remarks>
public static class ProviderInfrastructure
{
    public static void RegisterProviderInfrastructure(this BunitContext ctx)
    {
        var handler = new MockHttpHandler().WithDefault("[]");
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") });

        ctx.Services.AddSingleton(factory.Object);
        ctx.Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);

        var tokenProvider = new Mock<IAccessTokenProvider>();
        tokenProvider.Setup(p => p.GetAccessToken(It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);
        ctx.Services.AddSingleton(tokenProvider.Object);
    }
}
