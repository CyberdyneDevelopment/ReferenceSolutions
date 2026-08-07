using System;
using System.Collections.Generic;
using System.Linq;
using Azure.Security.KeyVault.Secrets;
using Xunit;
using Shouldly;
using ReferenceSecretManagers.AzureKeyVault.Services;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.AzureKeyVault.Commands;
using Fdw.Services.SecretManagers.AzureKeyVault.Configuration;
using Fdw.Services.SecretManagers.AzureKeyVault.CredentialTypes;

namespace ReferenceSecretManagers.AzureKeyVault.Tests.Services;

public class AzureKeyVaultSecretMetadataTests
{
    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Constructor_WithValidParameters_SetsAllProperties()
    {
        // Arrange
        var name = "MySecret";
        var version = "abc123";
        var createdOn = DateTimeOffset.UtcNow.AddDays(-10);
        var updatedOn = DateTimeOffset.UtcNow.AddDays(-1);
        var expiresOn = DateTimeOffset.UtcNow.AddDays(30);
        var enabled = true;
        var tags = new Dictionary<string, string>(StringComparer.Ordinal) { ["Environment"] = "Production" };
        var vaultName = "MyVault";

        // Act
        var sut = new AzureKeyVaultSecretMetadata(
            name,
            version,
            createdOn,
            updatedOn,
            expiresOn,
            enabled,
            tags,
            vaultName);

        // Assert
        sut.Name.ShouldBe(name);
        sut.Version.ShouldBe(version);
        sut.CreatedOn.ShouldBe(createdOn);
        sut.UpdatedOn.ShouldBe(updatedOn);
        sut.ExpiresOn.ShouldBe(expiresOn);
        sut.IsEnabled.ShouldBe(enabled);
        sut.VaultName.ShouldBe(vaultName);
        sut.Tags.ShouldContainKey("Environment");
        sut.Tags["Environment"].ShouldBe("Production");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Constructor_WithNullName_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            new AzureKeyVaultSecretMetadata(null!, null, null, null, null, true, null))
            .ParamName.ShouldBe("name");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Constructor_WithNullTags_UsesEmptyDictionary()
    {
        // Act
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Assert
        sut.Tags.ShouldNotBeNull();
        sut.Tags.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Key_ReturnsName()
    {
        // Arrange
        var name = "MySecret";
        var sut = new AzureKeyVaultSecretMetadata(name, null, null, null, null, true, null);

        // Act & Assert
        sut.Key.ShouldBe(name);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Container_ReturnsVaultName()
    {
        // Arrange
        var vaultName = "MyVault";
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null, vaultName);

        // Act & Assert
        sut.Container.ShouldBe(vaultName);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CreatedAt_ReturnsCreatedOnWhenSet()
    {
        // Arrange
        var createdOn = DateTimeOffset.UtcNow.AddDays(-10);
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, createdOn, null, null, true, null);

        // Act & Assert
        sut.CreatedAt.ShouldBe(createdOn);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CreatedAt_ReturnsMinValueWhenCreatedOnIsNull()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.CreatedAt.ShouldBe(DateTimeOffset.MinValue);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ModifiedAt_ReturnsUpdatedOnWhenSet()
    {
        // Arrange
        var updatedOn = DateTimeOffset.UtcNow.AddDays(-1);
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, updatedOn, null, true, null);

        // Act & Assert
        sut.ModifiedAt.ShouldBe(updatedOn);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ModifiedAt_ReturnsMinValueWhenUpdatedOnIsNull()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.ModifiedAt.ShouldBe(DateTimeOffset.MinValue);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ExpiresAt_ReturnsExpiresOnWhenSet()
    {
        // Arrange
        var expiresOn = DateTimeOffset.UtcNow.AddDays(30);
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, expiresOn, true, null);

        // Act & Assert
        sut.ExpiresAt.ShouldBe(expiresOn);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ExpiresAt_ReturnsNullWhenExpiresOnIsNull()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.ExpiresAt.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void CreatedBy_ReturnsNull()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.CreatedBy.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ModifiedBy_ReturnsNull()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.ModifiedBy.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsExpired_ReturnsFalseWhenExpiresOnIsNull()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.IsExpired.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsExpired_ReturnsFalseWhenExpiresOnIsInFuture()
    {
        // Arrange
        var expiresOn = DateTimeOffset.UtcNow.AddDays(30);
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, expiresOn, true, null);

        // Act & Assert
        sut.IsExpired.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsExpired_ReturnsTrueWhenExpiresOnIsInPast()
    {
        // Arrange
        var expiresOn = DateTimeOffset.UtcNow.AddDays(-1);
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, expiresOn, true, null);

        // Act & Assert
        sut.IsExpired.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsBinary_ReturnsFalse()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.IsBinary.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void IsDeleted_ReturnsFalse()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void RecoveryLevel_ReturnsDefaultValue()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.RecoveryLevel.ShouldBe("Recoverable+Purgeable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void EncryptionMethod_ReturnsAES256()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.EncryptionMethod.ShouldBe("AES-256");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Properties_ContainsProviderAndVaultType()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act
        var properties = sut.Properties;

        // Assert
        properties.ShouldContainKey("Provider");
        properties["Provider"].ShouldBe("AzureKeyVault");
        properties.ShouldContainKey("VaultType");
        properties["VaultType"].ShouldBe("Standard");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Properties_IncludesVersionWhenSet()
    {
        // Arrange
        var version = "abc123";
        var sut = new AzureKeyVaultSecretMetadata("MySecret", version, null, null, null, true, null);

        // Act
        var properties = sut.Properties;

        // Assert
        properties.ShouldContainKey("Version");
        properties["Version"].ShouldBe(version);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Properties_IncludesCreatedOnWhenSet()
    {
        // Arrange
        var createdOn = DateTimeOffset.UtcNow.AddDays(-10);
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, createdOn, null, null, true, null);

        // Act
        var properties = sut.Properties;

        // Assert
        properties.ShouldContainKey("CreatedOn");
        properties["CreatedOn"].ShouldBe(createdOn);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToString_ReturnsFormattedString()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", "v1", null, null, null, true, null);

        // Act
        var result = sut.ToString();

        // Assert
        result.ShouldContain("MySecret");
        result.ShouldContain("v1");
        result.ShouldContain("True");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToString_WithNullVersion_ReturnsFormattedString()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act
        var result = sut.ToString();

        // Assert
        result.ShouldContain("MySecret");
        result.ShouldContain("latest");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Equals_WithSameNameAndVersion_ReturnsTrue()
    {
        // Arrange
        var sut1 = new AzureKeyVaultSecretMetadata("MySecret", "v1", null, null, null, true, null);
        var sut2 = new AzureKeyVaultSecretMetadata("MySecret", "v1", null, null, null, false, null);

        // Act & Assert
        sut1.Equals(sut2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Equals_WithDifferentName_ReturnsFalse()
    {
        // Arrange
        var sut1 = new AzureKeyVaultSecretMetadata("MySecret1", "v1", null, null, null, true, null);
        var sut2 = new AzureKeyVaultSecretMetadata("MySecret2", "v1", null, null, null, true, null);

        // Act & Assert
        sut1.Equals(sut2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Equals_WithDifferentVersion_ReturnsFalse()
    {
        // Arrange
        var sut1 = new AzureKeyVaultSecretMetadata("MySecret", "v1", null, null, null, true, null);
        var sut2 = new AzureKeyVaultSecretMetadata("MySecret", "v2", null, null, null, true, null);

        // Act & Assert
        sut1.Equals(sut2).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", "v1", null, null, null, true, null);

        // Act & Assert
        sut.Equals(null).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void GetHashCode_WithSameNameAndVersion_ReturnsSameHashCode()
    {
        // Arrange
        var sut1 = new AzureKeyVaultSecretMetadata("MySecret", "v1", null, null, null, true, null);
        var sut2 = new AzureKeyVaultSecretMetadata("MySecret", "v1", null, null, null, false, null);

        // Act & Assert
        sut1.GetHashCode().ShouldBe(sut2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void GetHashCode_WithDifferentNameOrVersion_ReturnsDifferentHashCode()
    {
        // Arrange
        var sut1 = new AzureKeyVaultSecretMetadata("MySecret", "v1", null, null, null, true, null);
        var sut2 = new AzureKeyVaultSecretMetadata("MySecret", "v2", null, null, null, true, null);

        // Act & Assert
        sut1.GetHashCode().ShouldNotBe(sut2.GetHashCode());
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Tags_InterfaceImplementation_ReturnsKeysAsReadOnlyCollection()
    {
        // Arrange
        var tags = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Tag1"] = "Value1",
            ["Tag2"] = "Value2"
        };
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, tags);

        // Act
        var interfaceTags = ((Fdw.Services.SecretManagers.Abstractions.ISecretMetadata)sut).Tags;

        // Assert
        interfaceTags.Count.ShouldBe(2);
        interfaceTags.ShouldContain("Tag1");
        interfaceTags.ShouldContain("Tag2");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Properties_IncludesUpdatedOnWhenSet()
    {
        // Arrange
        var updatedOn = DateTimeOffset.UtcNow.AddDays(-1);
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, updatedOn, null, true, null);

        // Act
        var properties = sut.Properties;

        // Assert
        properties.ShouldContainKey("UpdatedOn");
        properties["UpdatedOn"].ShouldBe(updatedOn);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Properties_IncludesExpiresOnWhenSet()
    {
        // Arrange
        var expiresOn = DateTimeOffset.UtcNow.AddDays(30);
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, expiresOn, true, null);

        // Act
        var properties = sut.Properties;

        // Assert
        properties.ShouldContainKey("ExpiresOn");
        properties["ExpiresOn"].ShouldBe(expiresOn);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Properties_IncludesEnabledEntry()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, false, null);

        // Act
        var properties = sut.Properties;

        // Assert
        properties.ShouldContainKey("Enabled");
        properties["Enabled"].ShouldBe(false);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Properties_IncludesRecoveryLevelEntry()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act
        var properties = sut.Properties;

        // Assert
        properties.ShouldContainKey("RecoveryLevel");
        properties["RecoveryLevel"].ShouldBe("Recoverable+Purgeable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void DeletedOn_ReturnsNull()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.DeletedOn.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AccessPolicy_ReturnsNull()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.AccessPolicy.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void SizeInBytes_DefaultsToZero()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.SizeInBytes.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void AvailableVersions_DefaultsToEmpty()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.AvailableVersions.ShouldNotBeNull();
        sut.AvailableVersions.Count.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Container_ReturnsNullWhenVaultNameNotProvided()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act & Assert
        sut.Container.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Equals_WithNonMetadataObject_ReturnsFalse()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", "v1", null, null, null, true, null);

        // Act & Assert
        sut.Equals("not a metadata object").ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Equals_WithSelf_ReturnsTrue()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", "v1", null, null, null, true, null);

        // Act & Assert
        sut.Equals(sut).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Equals_WithNullVersionsBothNull_ReturnsTrue()
    {
        // Arrange
        var sut1 = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);
        var sut2 = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, false, null);

        // Act & Assert
        sut1.Equals(sut2).ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Constructor_WithDisabledState_SetsIsEnabledFalse()
    {
        // Act
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, false, null);

        // Assert
        sut.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ToString_WithDisabledState_ContainsFalse()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", "v1", null, null, null, false, null);

        // Act
        var result = sut.ToString();

        // Assert
        result.ShouldContain("False");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Properties_ExcludesVersionWhenNull()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act
        var properties = sut.Properties;

        // Assert
        properties.ShouldNotContainKey("Version");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Properties_ExcludesCreatedOnWhenNull()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act
        var properties = sut.Properties;

        // Assert
        properties.ShouldNotContainKey("CreatedOn");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Properties_ExcludesUpdatedOnWhenNull()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act
        var properties = sut.Properties;

        // Assert
        properties.ShouldNotContainKey("UpdatedOn");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Properties_ExcludesExpiresOnWhenNull()
    {
        // Arrange
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Act
        var properties = sut.Properties;

        // Assert
        properties.ShouldNotContainKey("ExpiresOn");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Constructor_WithAllNullOptionalParameters_SetsDefaults()
    {
        // Act
        var sut = new AzureKeyVaultSecretMetadata("MySecret", null, null, null, null, true, null);

        // Assert
        sut.Version.ShouldBeNull();
        sut.CreatedOn.ShouldBeNull();
        sut.UpdatedOn.ShouldBeNull();
        sut.ExpiresOn.ShouldBeNull();
        sut.VaultName.ShouldBeNull();
        sut.IsDeleted.ShouldBeFalse();
        sut.DeletedOn.ShouldBeNull();
        sut.RecoveryLevel.ShouldBe("Recoverable+Purgeable");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void Constructor_WithAllDatesSet_PropertiesDictionaryContainsAllDates()
    {
        // Arrange
        var createdOn = DateTimeOffset.UtcNow.AddDays(-10);
        var updatedOn = DateTimeOffset.UtcNow.AddDays(-1);
        var expiresOn = DateTimeOffset.UtcNow.AddDays(30);

        // Act
        var sut = new AzureKeyVaultSecretMetadata(
            "MySecret", "v1", createdOn, updatedOn, expiresOn, true, null);

        // Assert
        var properties = sut.Properties;
        properties.ShouldContainKey("Version");
        properties.ShouldContainKey("CreatedOn");
        properties.ShouldContainKey("UpdatedOn");
        properties.ShouldContainKey("ExpiresOn");
        properties.ShouldContainKey("Enabled");
        properties.ShouldContainKey("RecoveryLevel");
        properties.ShouldContainKey("Provider");
        properties.ShouldContainKey("VaultType");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorFromSecretProperties_SetsNameAndDefaults()
    {
        // Arrange - Use SecretProperties(string name) constructor
        var secretProps = new SecretProperties("TestSecret");

        // Act
        var sut = new AzureKeyVaultSecretMetadata(secretProps);

        // Assert
        sut.Name.ShouldBe("TestSecret");
        sut.IsEnabled.ShouldBeTrue(); // Enabled defaults to null -> true
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorFromSecretProperties_WithUri_SetsVaultName()
    {
        // Arrange - Use SecretProperties(Uri id) constructor with vault URI
        var secretId = new Uri("https://myvault.vault.azure.net/secrets/TestSecret/abc123");
        var secretProps = new SecretProperties(secretId);

        // Act
        var sut = new AzureKeyVaultSecretMetadata(secretProps);

        // Assert
        sut.Name.ShouldBe("TestSecret");
        sut.Version.ShouldBe("abc123");
        sut.VaultName.ShouldBe("myvault.vault.azure.net");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorFromSecretProperties_WithTags_CopiesTags()
    {
        // Arrange
        var secretProps = new SecretProperties("TestSecret");
        secretProps.Tags["env"] = "prod";
        secretProps.Tags["team"] = "platform";

        // Act
        var sut = new AzureKeyVaultSecretMetadata(secretProps);

        // Assert
        sut.Tags.Count.ShouldBe(2);
        sut.Tags["env"].ShouldBe("prod");
        sut.Tags["team"].ShouldBe("platform");
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorFromSecretProperties_WithExpiresOn_SetsExpiration()
    {
        // Arrange
        var secretProps = new SecretProperties("TestSecret");
        secretProps.ExpiresOn = DateTimeOffset.UtcNow.AddDays(30);

        // Act
        var sut = new AzureKeyVaultSecretMetadata(secretProps);

        // Assert
        sut.ExpiresOn.ShouldNotBeNull();
        sut.ExpiresAt.ShouldNotBeNull();
        sut.IsExpired.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Security")]
    public void ConstructorFromSecretProperties_WithEnabledFalse_SetsIsEnabledFalse()
    {
        // Arrange
        var secretProps = new SecretProperties("TestSecret");
        secretProps.Enabled = false;

        // Act
        var sut = new AzureKeyVaultSecretMetadata(secretProps);

        // Assert
        sut.IsEnabled.ShouldBeFalse();
    }
}
