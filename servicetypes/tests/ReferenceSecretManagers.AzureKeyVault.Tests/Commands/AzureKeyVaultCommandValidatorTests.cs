using Xunit;
using Shouldly;
using ReferenceSecretManagers.AzureKeyVault.Commands;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;

namespace ReferenceSecretManagers.AzureKeyVault.Tests.Commands;

public class AzureKeyVaultCommandValidatorTests
{
    private readonly AzureKeyVaultCommandValidator _sut;

    public AzureKeyVaultCommandValidatorTests()
    {
        _sut = new AzureKeyVaultCommandValidator();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithValidGetSecretCommand_Succeeds()
    {
        // Arrange
        var command = new AzureKeyVaultManagementCommand
        {
            CommandType = "GetSecret",
            SecretKey = "MySecret"
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithValidListSecretsCommand_Succeeds()
    {
        // Arrange
        var command = new AzureKeyVaultManagementCommand
        {
            CommandType = "ListSecrets"
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithEmptyCommandType_Fails()
    {
        // Arrange
        var command = new AzureKeyVaultManagementCommand
        {
            CommandType = string.Empty,
            SecretKey = "MySecret"
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("ManagementCommand type"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_GetSecretWithoutSecretKey_Fails()
    {
        // Arrange
        var command = new AzureKeyVaultManagementCommand
        {
            CommandType = "GetSecret",
            SecretKey = null
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("Secret key"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_SetSecretWithoutSecretKey_Fails()
    {
        // Arrange
        var command = new AzureKeyVaultManagementCommand
        {
            CommandType = "SetSecret",
            SecretKey = string.Empty
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("Secret key"));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_DeleteSecretWithoutSecretKey_Fails()
    {
        // Arrange
        var command = new AzureKeyVaultManagementCommand
        {
            CommandType = "DeleteSecret"
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_ListSecretsWithoutSecretKey_Succeeds()
    {
        // Arrange
        var command = new AzureKeyVaultManagementCommand
        {
            CommandType = "ListSecrets"
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithNullParameters_Fails()
    {
        // Arrange
        var command = new AzureKeyVaultManagementCommand
        {
            CommandType = "GetSecret",
            SecretKey = "MySecret",
            Parameters = null!
        };

        // Act
        var result = _sut.Validate(command);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(e => e.ErrorMessage.Contains("parameters"));
    }
}
