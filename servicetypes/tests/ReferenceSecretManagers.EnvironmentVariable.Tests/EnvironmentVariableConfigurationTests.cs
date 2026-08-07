using System;
using Fdw.Services.SecretManagers.EnvironmentVariable.Configuration;
using Fdw;
using Fdw.Services;
using Fdw.Services.SecretManagers;
using Fdw.Services.SecretManagers.EnvironmentVariable.Commands;

namespace ReferenceSecretManagers.EnvironmentVariable.Tests;

/// <summary>
/// Tests for <see cref="EnvironmentVariableConfiguration"/>: <see cref="EnvironmentVariableConfiguration.TargetEnum"/>
/// parsing, the <see cref="EnvironmentVariableConfiguration.Properties"/> projection, and
/// <see cref="EnvironmentVariableConfiguration.Validate"/> delegating to the validator.
/// </summary>
public class EnvironmentVariableConfigurationTests
{
    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void TargetEnumValidStringParsesToEnumValue()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration { Target = nameof(EnvironmentVariableTarget.Machine) };

        // Act
        var target = config.TargetEnum;

        // Assert
        target.ShouldBe(EnvironmentVariableTarget.Machine);
    }

    [Fact]
    [Trait("Priority", "P2")]
    [Trait("Category", "Configuration")]
    public void TargetEnumInvalidStringFallsBackToProcess()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration { Target = "NotARealTarget" };

        // Act
        var target = config.TargetEnum;

        // Assert
        target.ShouldBe(EnvironmentVariableTarget.Process);
    }

    [Fact]
    [Trait("Priority", "P3")]
    [Trait("Category", "Configuration")]
    public void PropertiesIncludesPrefixWhenSet()
    {
        // Arrange
        var config = new EnvironmentVariableConfiguration { Prefix = "FDW_SECRET_" };

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
        var config = new EnvironmentVariableConfiguration { Prefix = "FDW_SECRET_", Separator = "__" };

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
