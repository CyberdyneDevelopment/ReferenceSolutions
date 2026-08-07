using System;
using System.Collections.Generic;
using Xunit;
using Shouldly;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;

namespace ReferenceSecretManagers.AzureKeyVault.Tests.Configuration;

public class AzureKeyVaultConfigurationValidatorTests
{
    private readonly AzureKeyVaultConfigurationValidator _sut;

    public AzureKeyVaultConfigurationValidatorTests()
    {
        _sut = new AzureKeyVaultConfigurationValidator();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithValidManagedIdentityConfiguration_Succeeds()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithValidServicePrincipalConfiguration_Succeeds()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ServicePrincipal",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321",
            ClientSecret = "super-secret-value"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithValidCertificateConfiguration_Succeeds()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "Certificate",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321",
            CertificatePath = "/path/to/cert.pfx"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithMissingVaultUri_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            AuthenticationMethod = "ManagedIdentity"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("VaultUri"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithEmptyVaultUri_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = string.Empty,
            AuthenticationMethod = "ManagedIdentity"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("VaultUri"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithInvalidVaultUriScheme_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "http://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("valid Azure Key Vault URI"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithInvalidVaultUriDomain_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.example.com/",
            AuthenticationMethod = "ManagedIdentity"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("valid Azure Key Vault URI"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithMissingAuthenticationMethod_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("AuthenticationMethod"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithInvalidAuthenticationMethod_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "InvalidMethod"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("ManagedIdentity, ServicePrincipal, Certificate, DeviceCode"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_ServicePrincipalWithMissingTenantId_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ServicePrincipal",
            ClientId = "87654321-4321-4321-4321-210987654321",
            ClientSecret = "super-secret"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("TenantId"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_ServicePrincipalWithInvalidTenantId_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ServicePrincipal",
            TenantId = "not-a-guid",
            ClientId = "87654321-4321-4321-4321-210987654321",
            ClientSecret = "super-secret"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("TenantId") && e.ErrorMessage.Contains("GUID"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_ServicePrincipalWithMissingClientId_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ServicePrincipal",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientSecret = "super-secret"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("ClientId"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_ServicePrincipalWithInvalidClientId_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ServicePrincipal",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "not-a-guid",
            ClientSecret = "super-secret"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("ClientId") && e.ErrorMessage.Contains("GUID"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_ServicePrincipalWithMissingClientSecret_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ServicePrincipal",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("ClientSecret"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_ServicePrincipalWithShortClientSecret_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ServicePrincipal",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321",
            ClientSecret = "short"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("ClientSecret") && e.ErrorMessage.Contains("8 characters"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_CertificateWithMissingTenantId_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "Certificate",
            ClientId = "87654321-4321-4321-4321-210987654321",
            CertificatePath = "/path/to/cert.pfx"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("TenantId"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_CertificateWithMissingClientId_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "Certificate",
            TenantId = "12345678-1234-1234-1234-123456789012",
            CertificatePath = "/path/to/cert.pfx"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("ClientId"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_CertificateWithMissingCertificatePath_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "Certificate",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("CertificatePath"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_CertificateWithInvalidCertificatePathExtension_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "Certificate",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321",
            CertificatePath = "/path/to/cert.cer"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains(".pfx") || e.ErrorMessage.Contains(".p12"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_CertificateWithPfxExtension_Succeeds()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "Certificate",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321",
            CertificatePath = "/path/to/cert.pfx"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_CertificateWithP12Extension_Succeeds()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "Certificate",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321",
            CertificatePath = "/path/to/cert.p12"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_ManagedIdentityWithValidGuidManagedIdentityId_Succeeds()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            ManagedIdentityId = "11111111-1111-1111-1111-111111111111"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_ManagedIdentityWithValidResourceIdManagedIdentityId_Succeeds()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            ManagedIdentityId = "/subscriptions/12345678-1234-1234-1234-123456789012/resourceGroups/rg/providers/Microsoft.ManagedIdentity/userAssignedIdentities/id"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_ManagedIdentityWithInvalidManagedIdentityId_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            ManagedIdentityId = "invalid-id"
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("ManagedIdentityId"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithTimeoutBelowMinimum_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            Timeout = TimeSpan.FromMilliseconds(500)
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("Timeout"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithTimeoutAboveMaximum_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            Timeout = TimeSpan.FromMinutes(15)
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("Timeout"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithValidTimeout_Succeeds()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            Timeout = TimeSpan.FromSeconds(30)
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithMaxSecretsPerPageBelowMinimum_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            MaxSecretsPerPage = 0
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("MaxSecretsPerPage"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithMaxSecretsPerPageAboveMaximum_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            MaxSecretsPerPage = 30
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("MaxSecretsPerPage"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithValidMaxSecretsPerPage_Succeeds()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            MaxSecretsPerPage = 10
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithInvalidRetryPolicyMaxRetries_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            RetryPolicy = new Dictionary<string, object>(StringComparer.Ordinal) { ["MaxRetries"] = 15 }
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("RetryPolicy"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithInvalidRetryPolicyInitialDelay_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            RetryPolicy = new Dictionary<string, object>(StringComparer.Ordinal) { ["InitialDelay"] = TimeSpan.FromMilliseconds(50) }
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("RetryPolicy"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithValidRetryPolicy_Succeeds()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            RetryPolicy = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["MaxRetries"] = 3,
                ["InitialDelay"] = TimeSpan.FromSeconds(1),
                ["MaxDelay"] = TimeSpan.FromSeconds(30),
                ["BackoffMultiplier"] = 2.0
            }
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithInvalidHeaderName_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            AdditionalHeaders = new Dictionary<string, string>(StringComparer.Ordinal) { ["X Custom"] = "Value" }
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("AdditionalHeaders"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithNullHeaderValue_Fails()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            AdditionalHeaders = new Dictionary<string, string>(StringComparer.Ordinal) { ["X-Custom"] = null! }
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("AdditionalHeaders"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithValidAdditionalHeaders_Succeeds()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity",
            AdditionalHeaders = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["X-Custom-Header"] = "Value1",
                ["X-Another-Header"] = "Value2"
            }
        };

        // Act
        var result = _sut.Validate(config);

        // Assert
        result.IsValid.ShouldBeTrue();
    }
}
