using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.Http.Tests;

/// <summary>
/// TC-11: a Bearer connection names the secret holding its token, and the factory must report that
/// name so <c>HttpConnectionProvider</c> reads it; the protocol then sends it as
/// <c>Authorization: Bearer</c>. Before this, Bearer required no secret and nothing was ever sent.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class HttpConnectionFactoryBearerTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RequiredSecretsForABearerConnectionNamesItsTokenSecretAndManager()
    {
        // Arrange
        var factory = new HttpConnectionFactory(
            NullLogger<HttpConnectionFactory>.Instance,
            new NoClients(),
            NullLoggerFactory.Instance);
        var config = new HttpConnectionConfiguration
        {
            BaseUrl = "http://youtrack.example/api/",
            AuthenticationType = "Bearer",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["SecretManagerName"] = "MsSqlSecrets",
                ["TokenSecretName"] = "YOUTRACK_TOKEN"
            }
        };

        // Act
        var required = factory.RequiredSecrets(config);

        // Assert
        required.IsSuccess.ShouldBeTrue();
        var (certificate, password, key, manager) = required.Value;
        certificate.ShouldBeNull();
        password.ShouldBeNull();
        key.ShouldBe("YOUTRACK_TOKEN");
        manager.ShouldBe("MsSqlSecrets");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RequiredSecretsForABearerConnectionWithoutItsTokenSecretNameFailsNamingIt()
    {
        // Arrange: the token secret's property misspelled, so TokenSecretName is never set.
        var factory = new HttpConnectionFactory(
            NullLogger<HttpConnectionFactory>.Instance,
            new NoClients(),
            NullLoggerFactory.Instance);
        var config = new HttpConnectionConfiguration
        {
            BaseUrl = "http://youtrack.example/api/",
            AuthenticationType = "Bearer",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["SecretManagerName"] = "MsSqlSecrets",
                ["TokenSecret"] = "YOUTRACK_TOKEN"
            }
        };

        // Act
        var required = factory.RequiredSecrets(config);

        // Assert: refused, naming the property, rather than a connection that sends no token.
        required.IsSuccess.ShouldBeFalse();
        required.CurrentMessage.ShouldNotBeNull().ShouldContain("TokenSecretName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RequiredSecretsForAnAuthenticationTypeNoSecurityTypeAnswersToFails()
    {
        // Arrange: Bearer misspelled, so no security type matches it.
        var factory = new HttpConnectionFactory(
            NullLogger<HttpConnectionFactory>.Instance,
            new NoClients(),
            NullLoggerFactory.Instance);
        var config = new HttpConnectionConfiguration
        {
            BaseUrl = "http://youtrack.example/api/",
            AuthenticationType = "Bearr",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["SecretManagerName"] = "MsSqlSecrets",
                ["TokenSecretName"] = "YOUTRACK_TOKEN"
            }
        };

        // Act
        var required = factory.RequiredSecrets(config);

        // Assert: refused, naming the type, rather than read as no security.
        required.IsSuccess.ShouldBeFalse();
        required.CurrentMessage.ShouldNotBeNull().ShouldContain("Bearr");
    }

    // RequiredSecrets never creates a client; a request for one is a test bug, so it throws.
    private sealed class NoClients : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new NotSupportedException("RequiredSecrets creates no client.");
    }
}
