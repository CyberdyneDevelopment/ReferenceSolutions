using System;
using ReferenceSecretManagers.EnvironmentVariable.Services;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;

namespace ReferenceSecretManagers.EnvironmentVariable.Tests;

/// <summary>
/// Tests for <see cref="EnvironmentVariableSecretMetadata"/>: the always-unknown metadata surface
/// environment variables genuinely have (no version/expiry/audit trail) and the live SizeInBytes read.
/// </summary>
public class EnvironmentVariableSecretMetadataTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ConstructorNullKeyThrowsArgumentNullException()
    {
        // Act
        var act = () => new EnvironmentVariableSecretMetadata(null!, "VAR", EnvironmentVariableTarget.Process);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ConstructorNullEnvironmentVariableNameThrowsArgumentNullException()
    {
        // Act
        var act = () => new EnvironmentVariableSecretMetadata("key", null!, EnvironmentVariableTarget.Process);

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void PropertiesReflectEnvironmentVariableLimitations()
    {
        // Arrange
        var metadata = new EnvironmentVariableSecretMetadata("KEY", "PREFIX_KEY", EnvironmentVariableTarget.Process);

        // Assert
        metadata.Key.ShouldBe("KEY");
        metadata.Container.ShouldBe(nameof(EnvironmentVariableTarget.Process));
        metadata.Version.ShouldBeNull();
        metadata.CreatedAt.ShouldBe(DateTimeOffset.MinValue);
        metadata.ModifiedAt.ShouldBe(DateTimeOffset.MinValue);
        metadata.ExpiresAt.ShouldBeNull();
        metadata.CreatedBy.ShouldBeNull();
        metadata.ModifiedBy.ShouldBeNull();
        metadata.IsExpired.ShouldBeFalse();
        metadata.IsEnabled.ShouldBeTrue();
        metadata.IsBinary.ShouldBeFalse();
        metadata.Tags.ShouldBeEmpty();
        metadata.AvailableVersions.ShouldBeEmpty();
        metadata.AccessPolicy.ShouldBeNull();
        metadata.EncryptionMethod.ShouldBeNull();
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void PropertiesDictionaryContainsEnvironmentVariableNameAndTarget()
    {
        // Arrange
        var metadata = new EnvironmentVariableSecretMetadata("KEY", "PREFIX_KEY", EnvironmentVariableTarget.User);

        // Act
        var properties = metadata.Properties;

        // Assert
        properties["EnvironmentVariableName"].ShouldBe("PREFIX_KEY");
        properties["Target"].ShouldBe("User");
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void SizeInBytesReturnsZeroWhenVariableIsNotSet()
    {
        // Arrange
        var variableName = $"FDWTEST_{Guid.NewGuid():N}";
        var metadata = new EnvironmentVariableSecretMetadata("KEY", variableName, EnvironmentVariableTarget.Process);

        // Act
        var size = metadata.SizeInBytes;

        // Assert
        size.ShouldBe(0);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void SizeInBytesReflectsLiveEnvironmentVariableValue()
    {
        // Arrange
        var variableName = $"FDWTEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(variableName, "hello", EnvironmentVariableTarget.Process);
        try
        {
            var metadata = new EnvironmentVariableSecretMetadata("KEY", variableName, EnvironmentVariableTarget.Process);

            // Act
            var size = metadata.SizeInBytes;

            // Assert
            size.ShouldBe("hello".Length * sizeof(char));
        }
        finally
        {
            Environment.SetEnvironmentVariable(variableName, null, EnvironmentVariableTarget.Process);
        }
    }
}
