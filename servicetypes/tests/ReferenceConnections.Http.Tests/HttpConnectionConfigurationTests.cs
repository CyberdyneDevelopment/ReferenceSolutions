using System;
using System.Collections.Generic;
using Fdw.Services.Connections.Http;
using Fdw.Services.Connections.Http.Abstractions;
using Shouldly;
using Xunit;

namespace ReferenceConnections.Http.Tests;

public class HttpConnectionConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void DefaultValuesAreCorrect()
    {
        var sut = new HttpConnectionConfiguration();

        sut.ConnectionType.ShouldBe("Http");
        sut.BaseUrl.ShouldBe(string.Empty);
        sut.Protocol.ShouldBe("Rest");
        sut.TimeoutSeconds.ShouldBe(30);
        sut.ContentType.ShouldBeNull();
        // Why: authentication is now a selector + a KVP the chosen method parses — the default method is
        // "None" and its KVP is empty. SecretManagerName/SecretKeyName are no longer connection columns;
        // they are keys a secret-backed method declares in the AdditionalProperties KVP.
        sut.AuthenticationType.ShouldBe("None");
        sut.AdditionalProperties.ShouldNotBeNull();
        sut.AdditionalProperties.Count.ShouldBe(0);
        sut.Soap.ShouldBeNull();
        sut.Lifetime.ShouldBe("Scoped");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateWithValidConfigurationSucceeds()
    {
        var sut = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.example.com",
            TimeoutSeconds = 30,
            Protocol = "Rest"
        };

        var result = sut.Validate();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateWithEmptyBaseUrlReturnsError()
    {
        var sut = new HttpConnectionConfiguration
        {
            BaseUrl = "",
            TimeoutSeconds = 30,
            Protocol = "Rest"
        };

        var result = sut.Validate();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsValid.ShouldBeFalse();
        result.Value.Errors.ShouldContain(e => e.PropertyName == "BaseUrl");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateWithZeroTimeoutReturnsError()
    {
        var sut = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.example.com",
            TimeoutSeconds = 0,
            Protocol = "Rest"
        };

        var result = sut.Validate();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsValid.ShouldBeFalse();
        result.Value.Errors.ShouldContain(e => e.PropertyName == "TimeoutSeconds");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateWithNegativeTimeoutReturnsError()
    {
        var sut = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.example.com",
            TimeoutSeconds = -5,
            Protocol = "Rest"
        };

        var result = sut.Validate();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsValid.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateWithEmptyProtocolReturnsError()
    {
        var sut = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.example.com",
            TimeoutSeconds = 30,
            Protocol = ""
        };

        var result = sut.Validate();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsValid.ShouldBeFalse();
        result.Value.Errors.ShouldContain(e => e.PropertyName == "Protocol");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ValidateWithMultipleErrorsReturnsAllErrors()
    {
        var sut = new HttpConnectionConfiguration
        {
            BaseUrl = "",
            TimeoutSeconds = 0,
            Protocol = ""
        };

        var result = sut.Validate();

        result.IsSuccess.ShouldBeTrue();
        result.Value!.Errors.Count.ShouldBe(3);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void PropertiesCanBeSet()
    {
        var sut = new HttpConnectionConfiguration
        {
            BaseUrl = "https://example.com",
            Protocol = "Soap12",
            TimeoutSeconds = 60,
            ContentType = "application/xml",
            Lifetime = "Singleton"
        };

        sut.BaseUrl.ShouldBe("https://example.com");
        sut.Protocol.ShouldBe("Soap12");
        sut.TimeoutSeconds.ShouldBe(60);
        sut.ContentType.ShouldBe("application/xml");
        sut.Lifetime.ShouldBe("Singleton");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void AdditionalPropertiesCanBeSet()
    {
        var sut = new HttpConnectionConfiguration
        {
            AuthenticationType = "WsSecurity",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["SecretManagerName"] = "AzureKeyVault",
                ["CertificateSecretName"] = "my-cert"
            }
        };

        sut.AuthenticationType.ShouldBe("WsSecurity");
        sut.AdditionalProperties.ShouldNotBeNull();
        sut.AdditionalProperties["SecretManagerName"].ShouldBe("AzureKeyVault");
        sut.AdditionalProperties["CertificateSecretName"].ShouldBe("my-cert");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void SoapSettingsCanBeSet()
    {
        var sut = new HttpConnectionConfiguration
        {
            Soap = new HttpSoapSettings
            {
                DefaultNamespace = "http://example.com/ns",
                SoapActionPattern = "http://example.com/{operation}",
                Source = "FDW",
                UserId = "user1"
            }
        };

        sut.Soap.ShouldNotBeNull();
        sut.Soap.DefaultNamespace.ShouldBe("http://example.com/ns");
        sut.Soap.SoapActionPattern.ShouldBe("http://example.com/{operation}");
        sut.Soap.Source.ShouldBe("FDW");
        sut.Soap.UserId.ShouldBe("user1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExplicitInterfaceReturnsAdditionalProperties()
    {
        // Arrange
        var sut = new HttpConnectionConfiguration
        {
            AuthenticationType = "ApiKey",
            AdditionalProperties = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
            {
                ["SecretManagerName"] = "AzureKeyVault",
                ["ApiKeySecretName"] = "my-key"
            }
        };

        // Act - access through the interface
        HttpConnectionConfiguration interfaceRef = sut;
        var additionalProperties = interfaceRef.AdditionalProperties;

        // Assert
        additionalProperties.ShouldNotBeNull();
        additionalProperties.ContainsKey("ApiKeySecretName").ShouldBeTrue();
        interfaceRef.AuthenticationType.ShouldBe("ApiKey");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExplicitInterfaceReturnsEmptyAdditionalPropertiesWhenNotSet()
    {
        // Arrange
        var sut = new HttpConnectionConfiguration();

        // Act
        HttpConnectionConfiguration interfaceRef = sut;
        var additionalProperties = interfaceRef.AdditionalProperties;

        // Assert
        // Why: AdditionalProperties is initialized to an empty dict so the gateway cascade can populate
        // it from conn.HttpConnectionAuthentication rows without null-checks. Empty != null.
        additionalProperties.ShouldNotBeNull();
        additionalProperties.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExplicitInterfaceSoapReturnsSoapSettings()
    {
        // Arrange
        var sut = new HttpConnectionConfiguration
        {
            Soap = new HttpSoapSettings
            {
                DefaultNamespace = "http://example.com/soap",
                SoapActionPattern = "http://example.com/{operation}"
            }
        };

        // Act - access through the interface
        HttpConnectionConfiguration interfaceRef = sut;
        var soap = interfaceRef.Soap;

        // Assert
        soap.ShouldNotBeNull();
        soap.DefaultNamespace.ShouldBe("http://example.com/soap");
        soap.SoapActionPattern.ShouldBe("http://example.com/{operation}");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void ExplicitInterfaceSoapReturnsNullWhenNotSet()
    {
        // Arrange
        var sut = new HttpConnectionConfiguration();

        // Act
        HttpConnectionConfiguration interfaceRef = sut;
        var soap = interfaceRef.Soap;

        // Assert
        soap.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Api")]
    public void InterfaceBaseUrlMatchesConcreteBaseUrl()
    {
        // Arrange
        var sut = new HttpConnectionConfiguration
        {
            BaseUrl = "https://api.test.com"
        };

        // Act
        HttpConnectionConfiguration interfaceRef = sut;

        // Assert
        interfaceRef.BaseUrl.ShouldBe("https://api.test.com");
        interfaceRef.Protocol.ShouldBe("Rest");
        interfaceRef.TimeoutSeconds.ShouldBe(30);
        interfaceRef.ContentType.ShouldBeNull();
    }
}
