using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ReferenceNotifications.Teams.Tests;

/// <summary>
/// Test double for <see cref="HttpMessageHandler"/> that returns a scripted response (or a
/// caller-supplied response function) and records every outgoing request for assertion.
/// </summary>
internal sealed class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;
    private int _callCount;

    public MockHttpMessageHandler(HttpResponseMessage response)
        : this((_, _) => Task.FromResult(response))
    {
    }

    public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    public HttpRequestMessage? LastRequest { get; private set; }

    public int CallCount => _callCount;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _callCount);
        LastRequest = request;
        return _handler(request, cancellationToken);
    }
}
