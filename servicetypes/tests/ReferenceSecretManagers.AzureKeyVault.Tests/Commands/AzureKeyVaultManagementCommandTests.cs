using System;
using System.Collections.Generic;
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

public class AzureKeyVaultManagementCommandTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Constructor_CreatesInstanceWithDefaultValues()
    {
        // Act
        var sut = new AzureKeyVaultManagementCommand();

        // Assert
        sut.ShouldNotBeNull();
        sut.CommandId.ShouldNotBe(Guid.Empty);
        sut.CommandType.ShouldBe(string.Empty);
        sut.SecretKey.ShouldBe(string.Empty);
        sut.Parameters.ShouldNotBeNull();
        sut.Parameters.Count.ShouldBe(0);
        sut.Metadata.ShouldNotBeNull();
        sut.Metadata.Count.ShouldBe(0);
        sut.CorrelationId.ShouldNotBe(Guid.Empty);
        sut.CreatedAt.ShouldBeLessThanOrEqualTo(DateTime.UtcNow);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CommandId_ReturnsUniqueGuidForEachInstance()
    {
        // Act
        var sut1 = new AzureKeyVaultManagementCommand();
        var sut2 = new AzureKeyVaultManagementCommand();

        // Assert
        sut1.CommandId.ShouldNotBe(sut2.CommandId);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CommandId_ExplicitInterface_ReturnsGuidAsString()
    {
        // Act
        var sut = new AzureKeyVaultManagementCommand();
        var commandId = ((Fdw.Services.SecretManagers.Abstractions.ISecretManagerCommand)sut).CommandId;

        // Assert
        commandId.ShouldNotBeNullOrWhiteSpace();
        Guid.TryParse(commandId, out _).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsSecretModifying_ReturnsTrueForSetSecret()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand { CommandType = "SetSecret" };

        // Act & Assert
        sut.IsSecretModifying.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsSecretModifying_ReturnsTrueForDeleteSecret()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand { CommandType = "DeleteSecret" };

        // Act & Assert
        sut.IsSecretModifying.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsSecretModifying_ReturnsTrueForPurgeSecret()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand { CommandType = "PurgeSecret" };

        // Act & Assert
        sut.IsSecretModifying.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsSecretModifying_ReturnsTrueForRestoreSecret()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand { CommandType = "RestoreSecret" };

        // Act & Assert
        sut.IsSecretModifying.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsSecretModifying_ReturnsTrueForBackupSecret()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand { CommandType = "BackupSecret" };

        // Act & Assert
        sut.IsSecretModifying.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsSecretModifying_ReturnsFalseForGetSecret()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand { CommandType = "GetSecret" };

        // Act & Assert
        sut.IsSecretModifying.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsSecretModifying_ReturnsFalseForListSecrets()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand { CommandType = "ListSecrets" };

        // Act & Assert
        sut.IsSecretModifying.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand
        {
            CommandType = "GetSecret",
            SecretKey = "MySecret"
        };

        // Act
        var result = sut.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Validate_WithMissingCommandType_ReturnsFailure()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand
        {
            SecretKey = "MySecret"
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
    public void Validate_WithMissingSecretKeyForGetSecret_ReturnsFailure()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand
        {
            CommandType = "GetSecret"
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
    public void Validate_ListSecretsWithoutSecretKey_ReturnsSuccess()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand
        {
            CommandType = "ListSecrets"
        };

        // Act
        var result = sut.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void WithParameters_CreatesNewCommandWithUpdatedParameters()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand
        {
            CommandType = "GetSecret",
            SecretKey = "MySecret"
        };
        var newParams = new Dictionary<string, object?>(StringComparer.Ordinal) { ["Version"] = "v1" };

        // Act
        var result = sut.WithParameters(newParams);

        // Assert
        result.ShouldNotBeNull();
        result.CommandType.ShouldBe("GetSecret");
        result.SecretKey.ShouldBe("MySecret");
        result.Parameters.ShouldBe(newParams);
        result.Parameters["Version"].ShouldBe("v1");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void WithParameters_WithNullParameters_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => sut.WithParameters(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void WithMetadata_CreatesNewCommandWithUpdatedMetadata()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand
        {
            CommandType = "GetSecret",
            SecretKey = "MySecret"
        };
        var newMetadata = new Dictionary<string, object>(StringComparer.Ordinal) { ["Source"] = "API" };

        // Act
        var result = sut.WithMetadata(newMetadata);

        // Assert
        result.ShouldNotBeNull();
        result.CommandType.ShouldBe("GetSecret");
        result.SecretKey.ShouldBe("MySecret");
        result.Metadata.ShouldBe(newMetadata);
        result.Metadata["Source"].ShouldBe("API");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void WithMetadata_WithNullMetadata_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new AzureKeyVaultManagementCommand();

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => sut.WithMetadata(null!));
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AllProperties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var sut = new AzureKeyVaultManagementCommand
        {
            CommandType = "SetSecret",
            SecretKey = "MySecret",
            Parameters = new Dictionary<string, object?>(StringComparer.Ordinal) { ["Value"] = "SecretValue" },
            Container = "MyVault",
            ExpectedResultType = typeof(string),
            Timeout = TimeSpan.FromSeconds(30),
            Metadata = new Dictionary<string, object>(StringComparer.Ordinal) { ["User"] = "Admin" },
            CorrelationId = Guid.NewGuid()
        };

        // Assert
        sut.CommandType.ShouldBe("SetSecret");
        sut.SecretKey.ShouldBe("MySecret");
        sut.Parameters["Value"].ShouldBe("SecretValue");
        sut.Container.ShouldBe("MyVault");
        sut.ExpectedResultType.ShouldBe(typeof(string));
        sut.Timeout.ShouldBe(TimeSpan.FromSeconds(30));
        sut.Metadata["User"].ShouldBe("Admin");
        sut.CorrelationId.ShouldNotBe(Guid.Empty);
    }
}
