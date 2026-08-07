using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Reference.Ui.Tests.Infrastructure;

/// <summary>
/// HttpMessageHandler that returns preconfigured JSON responses keyed by URL
/// substring. Lets a bUnit test render an FDW provider component without standing
/// up its full HTTP stack.
/// </summary>
public sealed class MockHttpHandler : HttpMessageHandler
{
    private readonly Dictionary<string, (string Json, HttpStatusCode Status)> _responses
        = new(StringComparer.OrdinalIgnoreCase);

    private HttpStatusCode _defaultStatus = HttpStatusCode.OK;
    private string _defaultBody = "[]";
    public List<HttpRequestMessage> Requests { get; } = new();

    public MockHttpHandler RespondWith<T>(string urlContains, T body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses[urlContains] = (JsonSerializer.Serialize(body, JsonOpts), status);
        return this;
    }

    public MockHttpHandler WithDefault(string body, HttpStatusCode status = HttpStatusCode.OK)
    {
        _defaultBody = body;
        _defaultStatus = status;
        return this;
    }

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var url = request.RequestUri?.ToString() ?? string.Empty;
        foreach (var (key, (json, status)) in _responses)
        {
            if (url.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(new HttpResponseMessage(status)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                });
            }
        }
        return Task.FromResult(new HttpResponseMessage(_defaultStatus)
        {
            Content = new StringContent(_defaultBody, Encoding.UTF8, "application/json")
        });
    }
}
