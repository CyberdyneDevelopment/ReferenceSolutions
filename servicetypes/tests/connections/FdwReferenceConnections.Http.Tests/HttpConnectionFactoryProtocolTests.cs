using System;
using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.Http.Tests;

/// <summary>
/// FDW-2836: a connection states its protocol and none is assumed. A blank Protocol is refused with a message
/// naming the connection, where it used to be built as Rest; a Protocol no option answers to is still refused.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class HttpConnectionFactoryProtocolTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CreateWithABlankProtocolFailsNamingTheConnectionAndBuildsNothing(string protocol)
    {
        // Arrange: NoClients throws if a client is requested, so a refusal that built anything would fail the test.
        var factory = new HttpConnectionFactory(
            NullLogger<HttpConnectionFactory>.Instance,
            new NoClients(),
            NullLoggerFactory.Instance);
        var config = new HttpConnectionConfiguration
        {
            Name = "Orders",
            BaseUrl = "http://orders.example/api/",
            Protocol = protocol
        };

        // Act
        var created = factory.Create(config, certificate: null, password: null, apiKey: null);

        // Assert: refused, naming the connection, rather than built as Rest.
        created.IsSuccess.ShouldBeFalse();
        var message = created.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("No protocol specified");
        message.ShouldContain("Orders");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void CreateWithAProtocolNoOptionAnswersToFailsNamingIt()
    {
        // Arrange
        var factory = new HttpConnectionFactory(
            NullLogger<HttpConnectionFactory>.Instance,
            new NoClients(),
            NullLoggerFactory.Instance);
        var config = new HttpConnectionConfiguration
        {
            Name = "Orders",
            BaseUrl = "http://orders.example/api/",
            Protocol = "Resst"
        };

        // Act
        var created = factory.Create(config, certificate: null, password: null, apiKey: null);

        // Assert: the unknown name is reported, not the blank-protocol message.
        created.IsSuccess.ShouldBeFalse();
        var message = created.CurrentMessage.ShouldNotBeNull();
        message.ShouldContain("Resst");
        message.ShouldNotContain("No protocol specified");
    }

    // A refused connection creates no client; a request for one is a test bug, so it throws.
    private sealed class NoClients : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new NotSupportedException("A refused connection creates no client.");
    }
}
