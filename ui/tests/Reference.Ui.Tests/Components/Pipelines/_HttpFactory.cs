using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace Reference.Ui.Tests.Components.Pipelines;

/// <summary>
/// Method + URL-substring aware fake <see cref="HttpMessageHandler"/> for this area. Several
/// providers (ProjectProvider, StageProvider, ScheduleProvider) construct inline API clients from
/// a named HttpClient where the SAME path is used for a GET (list, array body) and a POST (create,
/// single-object body), so the shared <c>MockHttpHandler</c> (URL-only) cannot disambiguate them.
/// </summary>
internal sealed class AreaHttpHandler : HttpMessageHandler
{
    private readonly List<(HttpMethod? Method, string UrlContains, string Json, HttpStatusCode Status)> _rules = new();

    public List<HttpRequestMessage> Requests { get; } = new();

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public AreaHttpHandler On<T>(HttpMethod? method, string urlContains, T body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _rules.Add((method, urlContains, JsonSerializer.Serialize(body, JsonOpts), status));
        return this;
    }

    public AreaHttpHandler OnGet<T>(string urlContains, T body, HttpStatusCode status = HttpStatusCode.OK) =>
        On(HttpMethod.Get, urlContains, body, status);

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var url = request.RequestUri?.ToString() ?? string.Empty;
        // Why: iterate newest-first so a test-specific rule added after a default seed wins.
        for (var i = _rules.Count - 1; i >= 0; i--)
        {
            var (method, key, json, status) = _rules[i];
            if (method is not null && method != request.Method) continue;
            if (!url.Contains(key, StringComparison.OrdinalIgnoreCase)) continue;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
        // Why: default to an empty JSON array so unmatched list GETs deserialize cleanly.
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[]", Encoding.UTF8, "application/json")
        });
    }
}

/// <summary>
/// Builds an <see cref="IHttpClientFactory"/> whose every named client shares one handler.
/// </summary>
internal static class HttpFactory
{
    public static (IHttpClientFactory Factory, Reference.Ui.Tests.Infrastructure.MockHttpHandler Handler) Create(
        Action<Reference.Ui.Tests.Infrastructure.MockHttpHandler>? configure = null)
    {
        var handler = new Reference.Ui.Tests.Infrastructure.MockHttpHandler();
        configure?.Invoke(handler);
        return (Wrap(handler), handler);
    }

    public static (IHttpClientFactory Factory, AreaHttpHandler Handler) CreateArea(Action<AreaHttpHandler>? configure = null)
    {
        var handler = new AreaHttpHandler();
        configure?.Invoke(handler);
        return (Wrap(handler), handler);
    }

    private static IHttpClientFactory Wrap(HttpMessageHandler handler)
    {
        var mock = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://localhost/api/")
            });
        return mock.Object;
    }
}
