using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Fdw.Services.Connections.Http.Security;
using Microsoft.Extensions.Logging.Abstractions;

namespace FdwReferenceConnections.Http.Tests;

/// <summary>
/// FDW-2844: the factory keeps no list of authentication types. It asks the bound type for the secret names it reads, so each type's
/// answer reaches <c>RequiredSecrets</c> in the slot that type states.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class HttpConnectionFactorySecretsTests
{
    // A connection that sets every value its authentication type requires, each to a name of its own.
    private static HttpConnectionConfiguration Connection(string authenticationType)
    {
        var values = HttpAuthenticationTypes.ByName(authenticationType).RequiredProperties
            .ToDictionary(property => property, property => (string?)$"S_{property}", StringComparer.OrdinalIgnoreCase);
        return new HttpConnectionConfiguration
        {
            Name = "Orders",
            BaseUrl = "http://orders.example/api/",
            AuthenticationType = authenticationType,
            AdditionalProperties = values
        };
    }

    private static HttpConnectionFactory Factory() =>
        new(NullLogger<HttpConnectionFactory>.Instance, new NoClients(), NullLoggerFactory.Instance);

    [Theory]
    [InlineData("WsSecurity", "S_CertificateSecretName", null, null)]
    [InlineData("UsernameToken", null, "S_PasswordSecretName", null)]
    [InlineData("Basic", null, "S_PasswordSecretName", null)]
    [InlineData("ApiKey", null, null, "S_ApiKeySecretName")]
    [InlineData("Bearer", null, null, "S_TokenSecretName")]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RequiredSecretsReportsEachTypesSecretNamesInTheSlotItStates(string type, string? certificate, string? password, string? apiKey)
    {
        var required = Factory().RequiredSecrets(Connection(type));

        required.IsSuccess.ShouldBeTrue();
        var (certificateName, passwordName, apiKeyName, manager) = required.Value;
        certificateName.ShouldBe(certificate);
        passwordName.ShouldBe(password);
        apiKeyName.ShouldBe(apiKey);
        manager.ShouldBe("S_SecretManagerName");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void RequiredSecretsForAConnectionWithNoSecurityNamesNoSecret()
    {
        var required = Factory().RequiredSecrets(Connection("None"));

        required.IsSuccess.ShouldBeTrue();
        var (certificateName, passwordName, apiKeyName, _) = required.Value;
        certificateName.ShouldBeNull();
        passwordName.ShouldBeNull();
        apiKeyName.ShouldBeNull();
    }

    // RequiredSecrets never creates a client; a request for one is a test bug, so it throws.
    private sealed class NoClients : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => throw new NotSupportedException("RequiredSecrets creates no client.");
    }
}
