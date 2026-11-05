using FdwReferenceSecretManagers.EnvironmentVariable.Services;
using FdwReferenceSecretManagers.EnvironmentVariable.Targets;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Tests;

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
        var act = () => new EnvironmentVariableSecretMetadata(null!, "VAR", new ProcessTarget());

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ConstructorNullEnvironmentVariableNameThrowsArgumentNullException()
    {
        // Act
        var act = () => new EnvironmentVariableSecretMetadata("key", null!, new ProcessTarget());

        // Assert
        Should.Throw<ArgumentNullException>(act);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void PropertiesReflectEnvironmentVariableLimitations()
    {
        // Arrange
        var metadata = new EnvironmentVariableSecretMetadata("KEY", "PREFIX_KEY", new ProcessTarget());

        // Assert
        metadata.Key.ShouldBe("KEY");
        metadata.Container.ShouldBe("Process");
        metadata.Version.ShouldBeNull();
        metadata.CreatedAt.ShouldBeNull();
        metadata.ModifiedAt.ShouldBeNull();
        metadata.ExpiresAt.ShouldBeNull();
        metadata.CreatedBy.ShouldBeNull();
        metadata.ModifiedBy.ShouldBeNull();
        metadata.IsExpired.ShouldBeFalse();
        metadata.IsEnabled.ShouldBe(true);
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
        var metadata = new EnvironmentVariableSecretMetadata("KEY", "PREFIX_KEY", new UserTarget());

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
        var metadata = new EnvironmentVariableSecretMetadata("KEY", variableName, new ProcessTarget());

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
            var metadata = new EnvironmentVariableSecretMetadata("KEY", variableName, new ProcessTarget());

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
