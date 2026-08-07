using Xunit;
using Shouldly;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;

namespace ReferenceSecretManagers.AzureKeyVault.Tests.Configuration;

public class AzureKeyVaultConfigurationTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsValid_ReturnsTrueWhenVaultUriAndAuthenticationMethodAreSet()
    {
        // Arrange
        var sut = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity"
        };

        // Act & Assert
        sut.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsValid_ReturnsFalseWhenVaultUriIsNull()
    {
        // Arrange
        var sut = new AzureKeyVaultConfiguration
        {
            VaultUri = null,
            AuthenticationMethod = "ManagedIdentity"
        };

        // Act & Assert
        sut.IsValid.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsValid_ReturnsFalseWhenVaultUriIsEmpty()
    {
        // Arrange
        var sut = new AzureKeyVaultConfiguration
        {
            VaultUri = string.Empty,
            AuthenticationMethod = "ManagedIdentity"
        };

        // Act & Assert
        sut.IsValid.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsValid_ReturnsFalseWhenVaultUriIsWhitespace()
    {
        // Arrange
        var sut = new AzureKeyVaultConfiguration
        {
            VaultUri = "   ",
            AuthenticationMethod = "ManagedIdentity"
        };

        // Act & Assert
        sut.IsValid.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsValid_ReturnsFalseWhenAuthenticationMethodIsNull()
    {
        // Arrange
        var sut = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = null
        };

        // Act & Assert
        sut.IsValid.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithValidConfiguration_ReturnsSuccess()
    {
        // Arrange
        var sut = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/",
            AuthenticationMethod = "ManagedIdentity"
        };

        // Act
        var result = sut.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithMissingVaultUri_ReturnsFailure()
    {
        // Arrange
        var sut = new AzureKeyVaultConfiguration
        {
            AuthenticationMethod = "ManagedIdentity"
        };

        // Act
        var result = sut.Validate();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithMissingAuthenticationMethod_ReturnsFailure()
    {
        // Arrange
        var sut = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://test.vault.azure.net/"
        };

        // Act
        var result = sut.Validate();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithInvalidVaultUri_ReturnsFailure()
    {
        // Arrange
        var sut = new AzureKeyVaultConfiguration
        {
            VaultUri = "not-a-valid-uri",
            AuthenticationMethod = "ManagedIdentity"
        };

        // Act
        var result = sut.Validate();

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.CurrentMessage.ShouldNotBeNull();
    }
}
