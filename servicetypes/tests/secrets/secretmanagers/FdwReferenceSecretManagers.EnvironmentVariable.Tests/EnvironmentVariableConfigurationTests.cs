using FdwReferenceSecretManagers.EnvironmentVariable.Configuration;

namespace FdwReferenceSecretManagers.EnvironmentVariable.Tests;

/// <summary>
/// Tests for <see cref="EnvironmentVariableConfiguration"/>: <see cref="EnvironmentVariableConfiguration.ResolveTarget"/>
/// parsing, the <see cref="EnvironmentVariableConfiguration.Properties"/> projection, and
/// <see cref="EnvironmentVariableConfiguration.Validate"/> delegating to the validator.
/// </summary>
public class EnvironmentVariableConfigurationTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ResolveTargetValidStringParsesToEnumValue()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration { Target = nameof(EnvironmentVariableTarget.Machine) };

        // Act
        var target = config.ResolveTarget();

        // Assert
        target.IsSuccess.ShouldBeTrue();
        target.Value.ShouldBe(EnvironmentVariableTarget.Machine);
    }

    [Theory]
    [InlineData("")]
    [InlineData("NotARealTarget")]
    [InlineData("42")]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void ResolveTargetOfAValueThatIsNoTargetAnswersAFailureAndNeverThrows(string value)
    {
        // Arrange - the target is the caller's choice (FDW-3823): a value that is no target is refused, not read as Process, and not thrown.
        var config = new EnvironmentVariableConfiguration { Target = value };

        // Act
        var target = config.ResolveTarget();

        // Assert
        target.IsFailure.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Configuration")]
    public void PropertiesIncludesPrefixWhenSet()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration { Prefix = "FDW_SECRET_", StripPrefix = true };

        // Act
        var properties = config.Properties;

        // Assert
        properties[nameof(EnvironmentVariableConfiguration.Prefix)].ShouldBe("FDW_SECRET_");
        properties[nameof(EnvironmentVariableConfiguration.CaseSensitive)].ShouldBe(false);
        properties[nameof(EnvironmentVariableConfiguration.StripPrefix)].ShouldBe(true);
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Configuration")]
    public void PropertiesOmitsPrefixWhenBlank()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration { Prefix = "" };

        // Act
        var properties = config.Properties;

        // Assert
        properties.ContainsKey(nameof(EnvironmentVariableConfiguration.Prefix)).ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateValidConfigurationReturnsSuccess()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration { Prefix = "FDW_SECRET_", Separator = "__", Target = nameof(EnvironmentVariableTarget.Process) };

        // Act
        var result = config.Validate();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value!.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P1")]
    [Trait("Category", "Configuration")]
    public void ValidateMissingPrefixReturnsFailure()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration { Prefix = "", Separator = "__" };

        // Act
        var result = config.Validate();

        // Assert
        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Configuration")]
    public void IsValidIsAlwaysTrue()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration();

        // Act & Assert — Why: documented as always-true regardless of Prefix/Separator state; distinct
        // from Validate(), which does enforce the required fields.
        config.IsValid.ShouldBeTrue();
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Configuration")]
    public void ConfigurationNameIsEnvironmentVariable()
    {
        // Act & Assert
        EnvironmentVariableConfiguration.ConfigurationName.ShouldBe("EnvironmentVariable");
    }
}
