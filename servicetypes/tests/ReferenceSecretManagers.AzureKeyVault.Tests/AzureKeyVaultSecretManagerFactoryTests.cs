using Fdw.Abstractions;
using Fdw.Configuration;
using Fdw.Services.SecretManagers.Abstractions;
using ReferenceSecretManagers.AzureKeyVault;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using ReferenceSecretManagers.AzureKeyVault.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;
using Xunit;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;

namespace ReferenceSecretManagers.AzureKeyVault.Tests;

public class AzureKeyVaultSecretManagerFactoryTests
{
    private readonly AzureKeyVaultSecretManagerFactory _factory;

    public AzureKeyVaultSecretManagerFactoryTests()
    {
        _factory = new AzureKeyVaultSecretManagerFactory(
            NullLogger<AzureKeyVaultSecretManagerFactory>.Instance,
            NullLogger<AzureKeyVaultSecretManager>.Instance);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithNullLoggerThrows()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AzureKeyVaultSecretManagerFactory(
                null!,
                NullLogger<AzureKeyVaultSecretManager>.Instance));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorWithNullSecretManagerLoggerThrows()
    {
        Should.Throw<ArgumentNullException>(() =>
            new AzureKeyVaultSecretManagerFactory(
                NullLogger<AzureKeyVaultSecretManagerFactory>.Instance,
                null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerWithNullConfigurationReturnsFailure()
    {
        // Act
        var result = await _factory.CreateSecretManager((AzureKeyVaultConfiguration)null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerWithNullVaultUriReturnsFailure()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = null
        };

        // Act
        var result = await _factory.CreateSecretManager(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerWithEmptyVaultUriReturnsFailure()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = ""
        };

        // Act
        var result = await _factory.CreateSecretManager(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerWithWhitespaceVaultUriReturnsFailure()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "   "
        };

        // Act
        var result = await _factory.CreateSecretManager(config);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerWithValidConfigReturnsSuccess()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://myvault.vault.azure.net/"
        };

        // Act
        var result = await _factory.CreateSecretManager(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.ShouldBeAssignableTo<ISecretManager>();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerWithGenericConfigNullReturnsFailure()
    {
        // Act
        var result = await _factory.CreateSecretManager((IGenericConfiguration)null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerWithCorrectGenericConfigTypeReturnsSuccess()
    {
        // Arrange
        IGenericConfiguration config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://myvault.vault.azure.net/"
        };

        // Act
        var result = await _factory.CreateSecretManager(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CreateSyncWithValidConfigReturnsSuccess()
    {
        // Arrange
        var config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://myvault.vault.azure.net/"
        };

        // Act
        var result = _factory.Create(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CreateSyncWithNullConfigReturnsFailure()
    {
        // Act
        var result = _factory.Create((AzureKeyVaultConfiguration)null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IServiceFactoryCreateWithValidConfigReturnsSuccess()
    {
        // Arrange
        IGenericConfiguration config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://myvault.vault.azure.net/"
        };

        // Act
        var result = ((IServiceFactory<ISecretManager>)_factory).Create(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IServiceFactoryCreateGenericServiceReturnsSuccess()
    {
        // Arrange
        IGenericConfiguration config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://myvault.vault.azure.net/"
        };

        // Act
        var result = ((IServiceFactory)_factory).Create(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IServiceFactoryCreateGenericServiceWithNullConfigReturnsFailure()
    {
        // Act
        var result = ((IServiceFactory)_factory).Create(null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IServiceFactoryCreateTypedWithCompatibleTypeReturnsSuccess()
    {
        // Arrange
        IGenericConfiguration config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://myvault.vault.azure.net/"
        };

        // Act
        var result = ((IServiceFactory)_factory).Create<ISecretManager>(config);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IServiceFactoryCreateTypedWithNullConfigReturnsFailure()
    {
        // Act
        var result = ((IServiceFactory)_factory).Create<ISecretManager>(null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IServiceFactoryCreateTypedWithIncompatibleTypeReturnsFailure()
    {
        // Arrange
        IGenericConfiguration config = new AzureKeyVaultConfiguration
        {
            VaultUri = "https://myvault.vault.azure.net/"
        };

        // Act - Request IGenericConnection which AzureKeyVaultSecretManager does NOT implement
        var result = ((IServiceFactory)_factory).Create<Fdw.Services.Connections.Abstractions.IGenericConnection>(config);

        // Assert - Should fail because AzureKeyVaultSecretManager is not IGenericConnection
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public async Task CreateSecretManagerWithGenericConfigWrongTypeReturnsFailure()
    {
        // Arrange - Use a mock of IGenericConfiguration that is NOT AzureKeyVaultConfiguration
        var wrongConfig = new Moq.Mock<IGenericConfiguration>();

        // Act
        var result = await _factory.CreateSecretManager(wrongConfig.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IServiceFactoryCreateGenericServiceWithWrongConfigTypeReturnsFailure()
    {
        // Arrange
        var wrongConfig = new Moq.Mock<IGenericConfiguration>();

        // Act
        var result = ((IServiceFactory)_factory).Create(wrongConfig.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IServiceFactoryCreateGenericServiceFailurePropagatesMessages()
    {
        // Arrange - null config causes failure
        // Act
        var result = ((IServiceFactory)_factory).Create(null!);

        // Assert
        result.IsSuccess.ShouldBeFalse();
        result.Messages.ShouldNotBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IServiceFactoryTypedCreateWithWrongConfigTypeReturnsFailure()
    {
        // Arrange
        var wrongConfig = new Moq.Mock<IGenericConfiguration>();

        // Act
        var result = ((IServiceFactory<ISecretManager>)_factory).Create(wrongConfig.Object);

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }
}
